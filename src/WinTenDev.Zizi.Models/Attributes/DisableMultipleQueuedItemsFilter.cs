using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace WinTenDev.Zizi.Models.Attributes;

public class DisableMultipleQueuedItemsFilter : JobFilterAttribute, IClientFilter, IApplyStateFilter
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly CultureInfo EnUs = new CultureInfo("en-US");

    /// <summary>
    /// Convert arguments into a culture-aware string
    /// </summary>
    ///<see cref="https://gist.github.com/odinserj/a8332a3f486773baa009?permalink_comment_id=4048344#gistcomment-4048344"/><see>
    private static string ConvertArgument(object obj)
    {
        switch (obj)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
            case DateTime dt:
                return dt.ToString("o");// ISO8601 date format
            default:
                return (string)Convert.ChangeType(
                    obj,
                    typeof(string),
                    EnUs
                );// And force the rest to US English
        }
    }

    private static bool AddFingerprintIfNotExists(
        IStorageConnection connection,
        Job job
    )
    {
        var fingerprintKey = GetFingerprintKey(job);
        var finterprintLockKey = GetFingerprintLockKey(fingerprintKey);
        var distributedLock = connection.AcquireDistributedLock(finterprintLockKey, LockTimeout);
        using (distributedLock)
        {
            var fingerprint = connection.GetAllEntriesFromHash(fingerprintKey);

            if (fingerprint != null)
            {
                // Actual fingerprint found, returning.
                return false;
            }

            // Fingerprint does not exist, it is invalid (no `Timestamp` key),
            // or it is not actual (timeout expired).
            connection.SetRangeInHash(
                fingerprintKey,
                new Dictionary<string, string>
                {
                    { "Timestamp", DateTimeOffset.UtcNow.ToString("o") }
                }
            );

            return true;
        }
    }

    private static void RemoveFingerprint(
        IStorageConnection connection,
        Job job
    )
    {
        var fingerprintKey = GetFingerprintKey(job);
        var finterprintLockKey = GetFingerprintLockKey(fingerprintKey);
        using (connection.AcquireDistributedLock(finterprintLockKey, LockTimeout))
        using (var transaction = connection.CreateWriteTransaction())
        {
            transaction.RemoveHash(fingerprintKey);
            transaction.Commit();
        }
    }

    private static string GetFingerprintLockKey(string fingerprintKey)
    {
        return string.Format("{0}:lock", fingerprintKey);
    }

    private static string GetFingerprintKey(Job job)
    {
        return string.Format("fingerprint:{0}", GetFingerprint(job));
    }

    private static string GetFingerprint(Job job)
    {
        string parameters = string.Empty;
        if (job?.Args != null)
        {
            parameters = string.Join(".", job.Args.Select(ConvertArgument));
        }
        if (job?.Type == null ||
            job.Method == null)
        {
            return string.Empty;
        }

        //https://gist.github.com/odinserj/a8332a3f486773baa009#gistcomment-1898401
        var payload = $"{job.Type.FullName}.{job.Method.Name}.{parameters}";
        var hash = SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var fingerprint = Convert.ToBase64String(hash);
        return fingerprint;
    }

    public void OnCreating(CreatingContext filterContext)
    {
        if (!AddFingerprintIfNotExists(filterContext.Connection, filterContext.Job))
        {
            filterContext.Canceled = true;
        }
    }

    public void OnCreated(CreatedContext filterContext)
    {
        //do nothing
    }


    public void OnStateApplied(
        ApplyStateContext context,
        IWriteOnlyTransaction transaction
    )
    {
        if (context.NewState.Name.Equals(Hangfire.States.SucceededState.StateName) ||
            context.NewState.Name.Equals(Hangfire.States.FailedState.StateName))
        {
            RemoveFingerprint(context.Connection, context.BackgroundJob.Job);
        }
    }

    public void OnStateUnapplied(
        ApplyStateContext context,
        IWriteOnlyTransaction transaction
    )
    {
        // do nothing
    }
}