using System;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Types;

public class FloodCheck
{
    public string Guid { get; set; }
    public DateTime TimeStamp { get; set; }
    public Update Update { get; set; }
}

public class FloodCheckResult
{
    public int LastActivitiesCount { get; set; }
    public float FloodRate { get; set; }
    public bool IsFlood { get; set; }
}