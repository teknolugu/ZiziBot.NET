using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.LiteDB;
using Hangfire.MySql;
using Hangfire.Redis;
using Hangfire.Storage;
using Hangfire.Storage.SQLite;
using Serilog;
using StackExchange.Redis;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils;

public static class HangfireUtil
{
    public static void DeleteAllJobs()
    {
        var sw = Stopwatch.StartNew();

        Log.Information("Deleting previous Hangfire jobs..");
        var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        var numOfJobs = recurringJobs.Count;

        Parallel.ForEach(
            recurringJobs,
            (
                recurringJobDto,
                parallelLoopState,
                index
            ) => {
                var recurringJobId = recurringJobDto.Id;

                Log.Debug(
                    "Deleting jobId: {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );
                RecurringJob.RemoveIfExists(recurringJobId);

                Log.Debug(
                    "Delete succeeded {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );
            }
        );

        Log.Information(
            "Hangfire jobs successfully deleted. Total: {NumOfJobs}. Time: {Elapsed}",
            numOfJobs,
            sw.Elapsed
        );

        sw.Stop();
    }

    public static void DeleteJob(string jobId)
    {
        Log.Debug("Deleting job by ID: '{JobId}'", jobId);
        RecurringJob.RemoveIfExists(jobId);
        Log.Debug("Job '{JobId}' deleted successfully..", jobId);
    }

    public static MySqlStorage GetMysqlStorage(string connectionStr)
    {
        var options = new MySqlStorageOptions
        {
            // TransactionIsolationLevel = IsolationLevel.ReadCommitted,
            QueuePollInterval = TimeSpan.FromSeconds(15),
            JobExpirationCheckInterval = TimeSpan.FromHours(1),
            CountersAggregateInterval = TimeSpan.FromMinutes(5),
            PrepareSchemaIfNecessary = true,
            DashboardJobListLimit = 50000,
            TransactionTimeout = TimeSpan.FromMinutes(1),
            TablesPrefix = "_hangfire"
        };
        var storage = new MySqlStorage(connectionStr, options);
        return storage;
    }

    public static void RegisterJob(
        string jobId,
        Expression<Action> methodCall,
        Func<string> cronExpression,
        TimeZoneInfo timeZone = null,
        string queue = "default"
    )
    {
        var sw = Stopwatch.StartNew();

        Log.Debug("Registering Job with ID: {JobId}", jobId);
        RecurringJob.RemoveIfExists(jobId);
        RecurringJob.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            timeZone,
            queue
        );
        RecurringJob.Trigger(jobId);

        Log.Debug(
            "Registering Job {JobId} finish in {Elapsed}",
            jobId,
            sw.Elapsed
        );

        sw.Stop();
    }

    [Obsolete("Please consider use IRecurringJobManager or IBackgroundJobClient if possible")]
    public static void RegisterJob<T>(
        string jobId,
        Expression<Func<T, Task>> methodCall,
        Func<string> cronExpression,
        TimeZoneInfo timeZone = null,
        string queue = "default",
        bool fireAfterRegister = true
    )
    {
        var sw = Stopwatch.StartNew();

        Log.Debug("Registering Job with ID: {JobId}", jobId);
        RecurringJob.RemoveIfExists(jobId);
        RecurringJob.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            timeZone,
            queue
        );
        if (fireAfterRegister) RecurringJob.Trigger(jobId);

        Log.Debug(
            "Registering Job {JobId} finish in {Elapsed}",
            jobId,
            sw.Elapsed
        );

        sw.Stop();
    }

    public static void RegisterJob(
        string jobId,
        Expression<Func<Task>> methodCall,
        Func<string> cronExpression,
        TimeZoneInfo timeZone = null,
        string queue = "default"
    )
    {
        var sw = Stopwatch.StartNew();

        Log.Debug("Registering Job with ID: {JobId}", jobId);
        RecurringJob.RemoveIfExists(jobId);
        RecurringJob.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            timeZone,
            queue
        );
        RecurringJob.Trigger(jobId);

        Log.Debug(
            "Registering Job {JobId} finish in {Elapsed}",
            jobId,
            sw.Elapsed
        );

        sw.Stop();
    }

    [Obsolete("Please consider use IRecurringJobManager or IBackgroundJobClient for registering Hangfire Jobs if possible")]
    public static int TriggerJobs(string prefixId)
    {
        var sw = Stopwatch.StartNew();

        Log.Information("Loading Hangfire jobs..");
        var connection = JobStorage.Current.GetConnection();

        var recurringJobs = connection.GetRecurringJobs();
        var filteredJobs = recurringJobs.Where(dto => dto.Id.StartsWith(prefixId)).ToList();
        Log.Debug(
            "Found {Count} of {Count1}",
            filteredJobs.Count,
            recurringJobs.Count
        );

        var numOfJobs = filteredJobs.Count;

        Parallel.ForEach(
            filteredJobs,
            (
                recurringJobDto,
                parallelLoopState,
                index
            ) => {
                var recurringJobId = recurringJobDto.Id;

                Log.Debug(
                    "Triggering jobId: {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );
                RecurringJob.Trigger(recurringJobId);

                Log.Debug(
                    "Trigger succeeded {RecurringJobId}, Index: {Index}",
                    recurringJobId,
                    index
                );
            }
        );

        Log.Information(
            "Hangfire jobs successfully trigger. Total: {NumOfJobs}. Time: {Elapsed}",
            numOfJobs,
            sw.Elapsed
        );

        sw.Stop();

        return filteredJobs.Count;
    }

    public static SQLiteStorage GetSqliteStorage(string connectionString)
    {
        Log.Information("HangfireSqlite: {ConnectionString}", connectionString);

        connectionString.EnsureDirectory();

        var options = new SQLiteStorageOptions()
        {
            QueuePollInterval = TimeSpan.FromSeconds(10)
        };

        var storage = new SQLiteStorage(connectionString, options);
        return storage;
    }

    public static LiteDbStorage GetLiteDbStorage(string connectionString)
    {
        Log.Information("HangfireLiteDb: {ConnectionString}", connectionString);

        connectionString.EnsureDirectory();

        var options = new LiteDbStorageOptions()
        {
            QueuePollInterval = TimeSpan.FromSeconds(10)
        };

        var storage = new LiteDbStorage(connectionString, options);
        return storage;
    }

    public static ConnectionMultiplexer GetRedisConnectionMultiplexer(string connStr)
    {
        return ConnectionMultiplexer.Connect(GetRedisConnectionString(connStr));
    }

    public static RedisStorage GetRedisStorage(string connStr)
    {
        return new RedisStorage(connStr);
    }

    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"")]
    private static string GetRedisConnectionString(string redisConnStr)
    {
        var redisConnectionStr = redisConnStr;
        var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
        if (redisUrl != null)
        {
            var tokens = redisUrl.Split(':', '@');
            var redisHost = tokens.ElementAtOrDefault(3);
            var redisPort = tokens.ElementAtOrDefault(4);
            var redisPassword = tokens.ElementAtOrDefault(2);

            if (tokens.Length < 5)
                throw new RedisException("Please ensure REDIS_URL or use another Hangfire storage");

            redisConnectionStr = $"{redisHost}:{redisPort},password={redisPassword}";
        }

        Log.Information("Hangfire RedisConnection: {ConnStr}", redisConnectionStr);

        return redisConnectionStr;
    }
}