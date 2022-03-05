using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoreLinq;
using MySqlConnector;
using SerilogTimings;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// Data Backup service implementation
/// </summary>
public class DatabaseService
{
    private const string DataDir = "Storage/Data/";
    private readonly ILogger<DatabaseService> _logger;
    private readonly QueryService _queryService;

    /// <summary>
    /// Constructor of DataBackupService
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="queryService"></param>
    public DatabaseService(
        ILogger<DatabaseService> logger,
        QueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async Task<string> GetCurrentDbName()
    {
        var query = await _queryService
            .CreateMySqlFactory()
            .RunSqlQueryAsync<string>("select database() as current_db_name");

        var dbName = query.FirstOrDefault("db_backup");
        return dbName;
    }

    /// <summary>
    /// This function is used to backup data from database
    /// </summary>
    /// <returns></returns>
    public async Task<DataBackupInfo> BackupMySqlDatabase()
    {
        var op = Operation.Begin("Exporting Data");

        RemoveOldSqlBackup();

        var dbName = await GetCurrentDbName();
        _logger.LogInformation("Starting Export Database. Name: {DbName}", dbName);

        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd");
        var fileName = $"{dbName}_{timeStamp}.sql";
        var fullName = Path.Combine(DataDir, fileName);

        _logger.LogDebug("Creating MySql Connection..");
        var connection = _queryService.CreateMysqlConnectionCore();

        var cmd = new MySqlCommand
        {
            Connection = connection
        };

        var mb = new MySqlBackup(cmd)
        {
            ExportInfo =
            {
                AddDropTable = false,
                AddDropDatabase = false,
                ResetAutoIncrement = true
            }
        };

        mb.ExportProgressChanged += (
            sender,
            args
        ) => {
            _logger.LogDebug(
                "Processing backup Table: {TableName}. Table Rows: {Rows}. Current Index: {Index}",
                args.CurrentTableName,
                args.TotalRowsInCurrentTable,
                args.CurrentRowIndexInCurrentTable
            );
        };

        mb.ExportCompleted += (
            sender,
            args
        ) => {
            _logger.LogDebug(
                "Backup Table complete. Elapsed: {TableName}",
                args.TimeUsed
            );
        };

        _logger.LogDebug("Opening MySql Connection..");
        await connection.OpenAsync();

        _logger.LogDebug(
            "Exporting database {DbName} to file: {SqlFile}",
            dbName,
            fullName
        );
        mb.ExportToFile(fullName);

        var zipFileName = fullName.CreateZip(false);
        _logger.LogDebug(
            "Database: {DbName} exported to file: {SqlZip}",
            dbName,
            zipFileName
        );

        _logger.LogDebug("Closing MySql Connection..");
        await connection.CloseAsync();

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

        op.Complete();

        return backupInfo;
    }

    /// <summary>
    /// This method is used to remove an old previous backup.
    /// </summary>
    public void RemoveOldSqlBackup()
    {
        _logger.LogInformation("Deleting previous Data Backup..");

        var listFile = DataDir.EnumerateFiles()
            .Where((s) => s.Contains(".sql"));

        listFile.ForEach(filePath => filePath.DeleteFile());
    }

    public async Task FixTableCollation()
    {
        var op = Operation.Begin("Fix MySql table Collation");

        const string defaultCharSet = "utf8mb4";
        const string defaultCollation = "utf8mb4_unicode_ci";

        var tableInfos = await _queryService
            .CreateMySqlFactory()
            .FromTable("information_schema.tables")
            .Select(
                "table_schema",
                "table_name",
                "table_collation",
                "table_rows",
                "data_length",
                "auto_increment",
                "create_time",
                "update_time"
            ).WhereRaw("table_schema = database()")
            .OrderByDesc("table_name")
            .GetAsync<TableInfo>();

        var needFixes = tableInfos.Where
            (
                tableInfo =>
                    tableInfo.TableCollation?.NotContains("utf8mb4") ?? false
            )
            .ToList();

        if (needFixes.Count == 0)
        {
            _logger.LogInformation("No table Collation not need required to fix");
            op.Complete();

            return;
        }

        await needFixes.ForEachAsync(
            degreeOfParallel: 4,
            async tableInfo => {
                var tableName = tableInfo.TableName;

                var sql = $"ALTER TABLE {tableName} " +
                          "CONVERT TO CHARACTER " +
                          $"SET {defaultCharSet} " +
                          $"COLLATE {defaultCollation};";

                var query = await _queryService
                    .CreateMySqlFactory()
                    .StatementAsync(sql);

                _logger.LogInformation(
                    "Fixing table {TableName} result: {Result}",
                    tableName,
                    query
                );
            }
        );

        op.Complete();
    }
}