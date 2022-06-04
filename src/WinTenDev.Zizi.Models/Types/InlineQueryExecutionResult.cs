using System.Diagnostics;

namespace WinTenDev.Zizi.Models.Types;

public class InlineQueryExecutionResult
{
    public bool IsSuccess { get; set; }

    public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();
}
