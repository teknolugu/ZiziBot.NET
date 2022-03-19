using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JsonFlatFileDataStore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Realms;
using Serilog;
using SqlKata.Compilers;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Internals;

/// <summary>
/// This is class of Query Service
/// </summary>
public class QueryService
{
    private readonly ConnectionStrings _connectionStrings;

    /// <summary>
    /// Instantiate Query Service
    /// </summary>
    /// <param name="connectionStrings"></param>
    public QueryService(IOptionsSnapshot<ConnectionStrings> connectionStrings)
    {
        _connectionStrings = connectionStrings.Value;
    }

    /// <summary>
    /// Create MySQL query factory
    /// </summary>
    /// <returns></returns>
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
}