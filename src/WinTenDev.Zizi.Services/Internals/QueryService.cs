using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Humanizer;
using JsonFlatFileDataStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Entities;
using MySqlConnector;
using Realms;
using Serilog;
using SqlKata.Compilers;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Internals;

public class QueryService
{
    private readonly ILogger<QueryService> _logger;
    private readonly BotService _botService;
    private readonly ConnectionStrings _connectionStrings;

    public QueryService(
        IOptionsSnapshot<ConnectionStrings> connectionStrings,
        ILogger<QueryService> logger,
        BotService botService
    )
    {
        _logger = logger;
        _botService = botService;
        _connectionStrings = connectionStrings.Value;
    }

    public QueryFactory CreateMySqlFactory()
    {
        var mysqlConn = _connectionStrings.MySql;

        var compiler = new MySqlCompiler();
        var connection = new MySqlConnection(mysqlConn);
        var factory = new QueryFactory(connection, compiler)
        {
            Logger = sql => Log.Debug("SQLKata: {Sql}", sql)
        };

        return factory;
    }

    public MySqlConnection CreateMysqlConnectionCore()
    {
        var mysqlConn = _connectionStrings.MySql;

        var connection = new MySqlConnection(mysqlConn);

        return connection;
    }

    public DataStore CreateJsonDatastore()
    {
        var jsonFile = "Storage/Data/JsonFlatDatastore.json".EnsureDirectory();
        DataStore datastore;

        try
        {
            datastore = new DataStore(jsonFile, reloadBeforeGetCollection: true);
        }
        catch (Exception e)
        {
            datastore = new DataStore(jsonFile);
        }

        return datastore;
    }

    public IDocumentCollection<TEntity> GetJsonCollection<TEntity>() where TEntity : class
    {
        var collection = CreateJsonDatastore()
            .GetCollection<TEntity>();

        return collection;
    }

    public async Task<Realm> GetMongoRealmInstance()
    {
        var realmPath = DirUtil.PathCombine(true, "Storage/Data/Realm.realm").EnsureDirectory();
        var realmInstance = await Realm.GetInstanceAsync(
            new RealmConfiguration(realmPath)
            {
                ShouldDeleteIfMigrationNeeded = true
            }
        );

        return realmInstance;
    }

    #region C R U D

    public async Task<int> InsertAsync<TEntity>(TEntity entity)
    {
        var tableName = MapperUtil.ToTableName<TEntity>();
        var values = entity.ToDictionary();

        var insert = await CreateMySqlFactory()
            .FromTable(tableName)
            .InsertAsync(values);

        return insert;
    }

    public async Task<IEnumerable<TEntity>> GetAsync<TEntity>(object where)
    {
        var tableName = MapperUtil.ToTableName<TEntity>();
        var whereDictionary = where.ToDictionary();

        var query = await CreateMySqlFactory()
            .FromTable(tableName)
            .Where(whereDictionary)
            .GetAsync<TEntity>();

        return query;
    }

    #endregion

    public async Task MongoDbOpen(string databaseName)
    {
        var connectionString = _connectionStrings.MongoDb;

        await DB.InitAsync(databaseName, MongoClientSettings.FromConnectionString(connectionString));
    }

    private async Task<string> MongoDbGetDbName(bool sharedDb)
    {
        if (sharedDb) return "shared";

        var bot = await _botService.GetMeAsync();
        return bot.Username.Underscore().ToLower();
    }

    public async Task MongoDbInsertAsync<TEntity>(
        TEntity entity,
        bool sharedDb = true
    ) where TEntity : IEntity
    {
        var dbName = await MongoDbGetDbName(sharedDb);

        await MongoDbOpen(dbName);
        await DB.InsertAsync(entity);
    }
}
