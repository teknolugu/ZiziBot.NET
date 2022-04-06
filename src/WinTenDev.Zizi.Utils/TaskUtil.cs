using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

    public static Task ForEachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> body
    )
    {
        return Task.WhenAll(source.Select(body));
    }

    public static async Task AsyncParallelForEach<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task> body,
        int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
        TaskScheduler scheduler = null
    )
    {
        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };
        if (scheduler != null)
            options.TaskScheduler = scheduler;
        var block = new ActionBlock<T>(body, options);
        await foreach (var item in source)
            block.Post(item);
        block.Complete();
        await block.Completion;
    }

    public static Task AsyncParallelForEach<T>(
        this IEnumerable<T> source,
        Func<T, Task> body,
        int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
        TaskScheduler scheduler = null
    )
    {
        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };
        if (scheduler != null)
            options.TaskScheduler = scheduler;
        var block = new ActionBlock<T>(body, options);
        foreach (var item in source)
            block.Post(item);
        block.Complete();
        return block.Completion;
    }
}
