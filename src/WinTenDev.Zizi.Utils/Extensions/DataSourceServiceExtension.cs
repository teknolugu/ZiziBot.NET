using ClickHouse.Client.ADO;
using LiteDB.Async;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using MySqlConnector.Logging;
using RepoDb;
using Serilog;
using SqlKata.Compilers;
using SqlKata.Execution;
using WinTenDev.Zizi.Exceptions;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils.Extensions;

/// <summary>
/// Extensions of DataSource
/// </summary>
public static class DataSourceServiceExtension
{
    /// <summary>
    /// Add MySql Connection via SqlKata
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ConnectionStringNullOrEmptyException"></exception>
    public static IServiceCollection AddSqlKataMysql(this IServiceCollection services)
    {
        MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();

        services.AddScoped(provider => {
            var connectionStrings = provider.GetRequiredService<IOptionsSnapshot<ConnectionStrings>>().Value;
            var connectionStringsMySql = connectionStrings.MySql;

            if (connectionStringsMySql.IsNullOrEmpty())
                throw new ConnectionStringNullOrEmptyException("MySQL");

            var compiler = new MySqlCompiler();
            var connection = new MySqlConnection(connectionStringsMySql);
            var factory = new QueryFactory(connection, compiler)
            {
                Logger = sql => {
                    Log.Debug("MySql Exec: {Sql}", sql);
                }
            };

            return factory;
        });

        return services;
    }

    /// <summary>
    /// Add LiteDB Connection
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddLiteDb(this IServiceCollection services)
    {
        services.AddScoped(_ => {
            var dbPath = "Storage/Data/Local_LiteDB.db";
            Log.Debug("Loading LiteDB: {DbPath}", dbPath);
            var dbName = dbPath.EnsureDirectory();
            var connectionString = $"Filename={dbName};Connection=shared;";

            return new LiteDatabaseAsync(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Add ClickHouse Connection
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddClickHouse(this IServiceCollection services)
    {
        return services.AddScoped(provider => {
            var connectionStrings = provider.GetRequiredService<IOptionsSnapshot<ConnectionStrings>>().Value;
            var connectionStringsClickHouseConn = connectionStrings.ClickHouseConn;

            if (connectionStringsClickHouseConn.IsNullOrEmpty())
                throw new ConnectionStringNullOrEmptyException("ClickHouse");

            return new ClickHouseConnection(connectionStringsClickHouseConn);
        });
    }

    public static IServiceCollection AddRepoDb(this IServiceCollection services)
    {
        MySqlConnectorBootstrap.Initialize();

        return services;
    }
}