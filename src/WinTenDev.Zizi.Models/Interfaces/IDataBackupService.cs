using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Interfaces;

/// <summary>
/// Interfaces of DataBackupService
/// </summary>
public interface IDataBackupService
{
    /// <summary>
    /// This function is used to backup data from database
    /// </summary>
    /// <returns></returns>
    Task<DataBackupInfo> BackupMySqlDatabase();

    void RemoveOldSqlBackup();
}