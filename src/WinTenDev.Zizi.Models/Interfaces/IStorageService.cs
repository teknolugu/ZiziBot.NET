using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Interfaces;

/// <summary>
/// Interface of the StorageService
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Log management for delete old log and upload to channel
    /// </summary>
    Task ClearLog();
    /// <summary>
    /// Hangfire storage reset
    /// </summary>
    Task ResetHangfire(ResetTableMode resetTableMode = ResetTableMode.Truncate);
}