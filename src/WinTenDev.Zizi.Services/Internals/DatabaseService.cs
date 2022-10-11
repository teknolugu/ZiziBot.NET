using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Humanizer;
using Ionic.Zip;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;
using MoreLinq;
using MySqlConnector;
using SerilogTimings;
using SqlKata.Execution;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace WinTenDev.Zizi.Services.Internals;

public class DatabaseService
{
    private const string DataDir = "Storage/Data/";
    private readonly ILogger<DatabaseService> _logger;
    private readonly EventLogConfig _eventLogConfig;
    private readonly ITelegramBotClient _botClient;
    private readonly BotService _botService;
    private readonly ConnectionStrings _connectionStrings;
    private readonly QueryService _queryService;

    public DatabaseService(
        ILogger<DatabaseService> logger,
        IOptionsSnapshot<EventLogConfig> eventLogConfig,
        IOptions<ConnectionStrings> connectionStrings,
        ITelegramBotClient botClient,
        BotService botService,
        QueryService queryService
    )
    {
        _logger = logger;
        _eventLogConfig = eventLogConfig.Value;
        _botClient = botClient;
        _botService = botService;
        _connectionStrings = connectionStrings.Value;
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

        var zipFileName = fullName.CreateZip(compressionMethod: CompressionMethod.BZip2);
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

    [JobDisplayName("MySQL AutoBackup")]
    public async Task AutomaticMysqlBackup()
    {
        var dataBackupInfo = await BackupMySqlDatabase();
        var fullNameZip = dataBackupInfo.FullNameZip;
        var fileNameZip = dataBackupInfo.FileNameZip;
        var channelTarget = _eventLogConfig.ChannelId;

        var caption = HtmlMessage.Empty
            .Bold("File Size: ").CodeBr($"{dataBackupInfo.FileSizeSql}")
            .Bold("Zip Size: ").CodeBr($"{dataBackupInfo.FileSizeSqlZip}")
            .Text("#mysql #auto #backup");

        await using var fileStream = File.OpenRead(fullNameZip);

        var media = new InputOnlineFile(fileStream, fileNameZip)
        {
            FileName = fileNameZip
        };

        await _botClient.SendDocumentAsync(
            chatId: channelTarget,
            document: media,
            caption: caption.ToString(),
            parseMode: ParseMode.Html
        );
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

    public async Task MongoDbDatabaseMapping()
    {
        var op = Operation.Begin("Mapping MongoDb Database");

        var meUser = await _botService.GetMeAsync();
        var meUsername = meUser.FirstName.Underscore();

        var connectionString = _connectionStrings.MongoDb;

        await DB.InitAsync(meUsername, MongoClientSettings.FromConnectionString(connectionString));
        await DB.InitAsync("shared", MongoClientSettings.FromConnectionString(connectionString));

        DB.DatabaseFor<AfkEntity>(meUsername);
        DB.DatabaseFor<ArticleSentEntity>(meUsername);
        DB.DatabaseFor<ChatSettingEntity>(meUsername);
        DB.DatabaseFor<ForceSubscriptionEntity>(meUsername);
        DB.DatabaseFor<GroupAdminEntity>(meUsername);
        DB.DatabaseFor<RssSourceEntity>(meUsername);
        DB.DatabaseFor<SpellEntity>(meUsername);
        DB.DatabaseFor<UserInfoEntity>(meUsername);
        DB.DatabaseFor<UserExceptionEntity>(meUsername);
        DB.DatabaseFor<WarnMemberEntity>(meUsername);
        DB.DatabaseFor<WebHookChatEntity>(meUsername);
        DB.DatabaseFor<WTelegramSessionEntity>(meUsername);

        DB.DatabaseFor<SubsceneSource>("shared");
        DB.DatabaseFor<SubsceneMovieSearch>("shared");
        DB.DatabaseFor<SubsceneMovieItem>("shared");
        DB.DatabaseFor<SubsceneSubtitleItem>("shared");

        op.Complete();
    }

    public async Task MongoDbEnsureCollectionIndex()
    {
        _logger.LogInformation("Creating MongoDb Index..");

        await DB.Index<RssSourceEntity>()
            .Key(entity => entity.ChatId, KeyType.Ascending)
            .Key(entity => entity.UrlFeed, KeyType.Ascending)
            .Option(options => options.Unique = true)
            .CreateAsync();

        await DB.Index<SpellEntity>()
            .Key(entity => entity.Typo, KeyType.Ascending)
            .Option(options => options.Unique = true)
            .CreateAsync();

        await DB.Index<SubsceneMovieItem>()
            .Key(item => item.MovieUrl, KeyType.Ascending)
            .Option(
                options =>
                    options.Unique = true
            )
            .CreateAsync();

        await DB.Index<SubsceneMovieSearch>()
            .Key(search => search.MovieUrl, KeyType.Ascending)
            .Option(
                options =>
                    options.Unique = true
            )
            .CreateAsync();

        await DB.Index<SubsceneSubtitleItem>()
            .Key(item => item.MovieUrl, KeyType.Ascending)
            .Option(options => options.Unique = true)
            .CreateAsync();

        _logger.LogInformation("Creating MongoDb Index complete");
    }

    [JobDisplayName("MongoDB AutoBackup")]
    public async Task MongoDbExport()
    {
        await MongoDbExportCore<AfkEntity>("csv");
        await MongoDbExportCore<ForceSubscriptionEntity>("csv");
        await MongoDbExportCore<GroupAdminEntity>("csv");
        await MongoDbExportCore<SpellEntity>("csv");
        await MongoDbExportCore<WarnMemberEntity>("csv");
        await MongoDbExportCore<WebHookChatEntity>("csv");

        var dirStamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var srcPath = Path.Combine("Storage", "Data", "MongoExport", dirStamp);
        var zipFileName = "MongoExport_" + dirStamp + ".zip";
        var filePath = Path.Combine("Storage", "Data", "MongoExport", zipFileName);

        var saveTo = srcPath
            .EnumerateFiles()
            .CreateZip(filePath, true);

        var channelTarget = _eventLogConfig.ChannelId;

        var fileSize = saveTo.FileSize();
        var dirSize = srcPath.DirSize();

        var caption = HtmlMessage.Empty
            .Bold("File Size: ").CodeBr($"{dirSize.SizeFormat()}")
            .Bold("Zip Size: ").CodeBr($"{fileSize.SizeFormat()}")
            .Text("#mongodb #auto #backup");

        await using var fileStream = File.OpenRead(saveTo);

        var media = new InputOnlineFile(fileStream, filePath)
        {
            FileName = zipFileName
        };

        await _botClient.SendDocumentAsync(
            chatId: channelTarget,
            document: media,
            caption: caption.ToString(),
            parseMode: ParseMode.Html
        );

        fileStream.Close();

        saveTo.DeleteFile();
        srcPath.DeleteDirectory();
    }

    private async Task MongoDbExportCore<T>(string fileType = "json") where T : IEntity
    {
        var collection = DB.Collection<T>();

        var collectionName = collection.CollectionNamespace.CollectionName;
        var fileName = collectionName + $".{fileType}";
        var dirStamp = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var filePath = Path.Combine("Storage", "Data", "MongoExport", dirStamp, fileName).EnsureDirectory();

        _logger.LogInformation("Exporting MongoDb Collection {CollectionName} to {FilePath}", collectionName, filePath);

        if (fileType == "csv")
        {
            var jsonPath = filePath.ReplaceExt("csv");
            var rows = await DB.Find<T>().ExecuteAsync();

            rows.WriteRecords(jsonPath, hasHeader: true);
        }
        else
        {
            await using var streamWriter = new StreamWriter(filePath);
            await collection.Find(new BsonDocument())
                .ForEachAsync(async (document) => {
                    await using var stringWriter = new StringWriter();
                    using var jsonWriter = new JsonWriter(stringWriter);

                    var context = BsonSerializationContext.CreateRoot(jsonWriter);
                    collection.DocumentSerializer.Serialize(context, document);
                    var line = stringWriter.ToString();

                    await streamWriter.WriteLineAsync(line);
                });
        }
    }
}