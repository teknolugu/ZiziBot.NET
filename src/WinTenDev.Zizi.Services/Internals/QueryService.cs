using Microsoft.Extensions.Options;
using MySqlConnector;
using Serilog;
using SqlKata.Compilers;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Configs;

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
    public QueryService(
        IOptionsSnapshot<ConnectionStrings> connectionStrings
    )
    {
        _connectionStrings = connectionStrings.Value;
    }

    /// <summary>
    /// Create MySQL query factory
    /// </summary>
    /// <returns></returns>
    public QueryFactory CreateMySqlConnection()
    {
        var mysqlConn = _connectionStrings.MySql;

        var compiler = new MySqlCompiler();
        var connection = new MySqlConnection(mysqlConn);
        var factory = new QueryFactory(connection, compiler)
        {
            Logger = sql => { Log.Debug("MySql Exec: {Sql}", sql); }
        };

        return factory;
    }

    public MySqlConnection CreateMysqlConnectionCore()
    {
        var mysqlConn = _connectionStrings.MySql;

        var connection = new MySqlConnection(mysqlConn);

        return connection;
    }
}