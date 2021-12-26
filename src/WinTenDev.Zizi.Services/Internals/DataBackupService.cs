using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using MySqlConnector;
using SerilogTimings;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// Data Backup service implementation
/// </summary>
public class DataBackupService : IDataBackupService
{
    private readonly string dataDir = "Storage/Data/";
    private readonly ILogger<DataBackupService> _logger;
    private readonly ConnectionStrings _connectionStrings;

    /// <summary>
    /// Constructor of DataBackupService
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="connectionStrings"></param>
    public DataBackupService(
        ILogger<DataBackupService> logger,
        IOptionsSnapshot<ConnectionStrings> connectionStrings)
    {
        _logger = logger;
        _connectionStrings = connectionStrings.Value;
    }

    /// <summary>
    /// This function is used to backup data from database
    /// </summary>
    /// <returns></returns>
    public async Task<DataBackupInfo> BackupMySqlDatabase()
    {
        var op = Operation.Begin("Exporting Data");

        RemoveOldSqlBackup();

        _logger.LogInformation("Starting Export Data..");

        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd");
        var fileName = $"db_backup_{timeStamp}.sql";
        var fullName = $"Storage/Data/db_backup_{timeStamp}.sql";
        var connectionStr = _connectionStrings.MySql;

        _logger.LogDebug("Creating DB Connection..");
        var connection = new MySqlConnection(connectionStr);
        var cmd = new MySqlCommand();
        var mb = new MySqlBackup(cmd);

        cmd.Connection = connection;

        _logger.LogDebug("Opening DB Connection..");
        await connection.OpenAsync();

        _logger.LogDebug("Exporting DB to file");
        mb.ExportToFile(fullName);

        var zipFileName = fullName.CreateZip(false);

        _logger.LogDebug("Closing DB Connection..");
        await connection.CloseAsync();

        op.Complete();

        var backupInfo = new DataBackupInfo
        {
            FileName = fileName,
            FullName = fullName,
            FileNameZip = zipFileName,
            FullNameZip = zipFileName,
            FileSizeSql = fullName.FileSize().SizeFormat(),
            FileSizeSqlRaw = fullName.FileSize(),
            FileSizeSqlZip = zipFileName.FileSize().SizeFormat(),
            FileSizeSqlZipRaw = zipFileName.FileSize()
        };

        return backupInfo;
    }

    /// <summary>
    /// This method is used to remove an old previous backup.
    /// </summary>
    public void RemoveOldSqlBackup()
    {
        _logger.LogInformation("Deleting previous Data Backup..");
        var listFile = dataDir.EnumerateFiles()
            .Where((s) => s.Contains("db_backup"));

        listFile.ForEach(filePath => filePath.DeleteFile());
    }
}