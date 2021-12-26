using System.Collections.Generic;
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
}