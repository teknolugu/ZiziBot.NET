using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Nito.AsyncEx;
using Serilog;

namespace WinTenDev.Zizi.Utils;

public static class TaskUtil
{
    public static void IgnoreAll(this List<Task> tasks)
    {
        Log.Debug("Ignoring {Count}", "task".ToQuantity(tasks.Count));
        tasks.ForEach(task => task.Ignore());
    }

    public static void InBackground(this Task task)
    {
        Task.Run(() => task);
    }

    public static void InBackgroundAll(this List<Task> tasks)
    {
        Log.Debug("Running {Count} in background", "task".ToQuantity(tasks.Count));
        tasks.ForEach(task => task.InBackground());
    }

    public static Task ForEachAsync<T>(
        this IEnumerable<T> source,
        int degreeOfParallel,
        Func<T, Task> body
    )
    {
        return Task.WhenAll
        (
            from partition in Partitioner.Create(source).GetPartitions(degreeOfParallel)
            select Task.Run
            (
                async delegate {
                    using (partition)
                    {
                        while (partition.MoveNext())
                            await body(partition.Current);
                    }
                }
            )
        );
    }
}