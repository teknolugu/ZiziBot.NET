using System;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Builders;
using FluentMigrator.Builders.Alter.Table;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Builders.Execute;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.DbMigrations.Extensions;

public static class FluentMigrationExtension
{
    public static IServiceCollection AddFluentMigration(
        this IServiceCollection services,
        string connectionString = null
    )
    {
        var connStr = connectionString;
        if (connectionString == null)
        {

            var serviceProvider = services.BuildServiceProvider();
            var connectionStrings = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStrings>>().Value;

            connStr = connectionStrings.MySql;
        }

        services
            .AddFluentMigratorCore()
            .ConfigureRunner
            (
                rb => rb
                    .AddMySql5()
                    .WithGlobalConnectionString(connStr)
                    .ScanIn(Assembly.GetAssembly(typeof(FluentMigrationExtension))).For.All()
            )
            .AddLogging
            (
                lb => lb
                    .AddSerilog()
                    .AddDebug()
            )
            .Configure<LogFileFluentMigratorLoggerOptions>
            (
                options => {
                    options.ShowSql = true;
                }
            );

        return services;
    }

    public static IApplicationBuilder UseFluentMigration(this IApplicationBuilder app)
    {
        Log.Information("Running DB migration..");

        var connectionStrings = app.GetRequiredService<IOptionsSnapshot<ConnectionStrings>>().Value;
        var services = new ServiceCollection();

        services.AddFluentMigration(connectionStrings.MySql);

        var runner = services.GetRequiredService<IMigrationRunner>();

        runner.ListMigrations();

        Log.Debug("Running MigrateUp");
        runner.MigrateUp();

        return app;
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithIdColumn(this ICreateTableWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("id")
            .AsInt64()
            .NotNullable()
            .PrimaryKey()
            .Identity();
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithTimeStamp(this ICreateTableWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("created_at").AsDateTime().NotNullable();
    }

    public static ICreateTableColumnOptionOrWithColumnSyntax WithTimeStamps(this ICreateTableWithColumnSyntax tableWithColumnSyntax)
    {
        return tableWithColumnSyntax
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
    }

    public static TNext AsMySqlText<TNext>(this IColumnTypeSyntax<TNext> createTableColumnAsTypeSyntax)
        where TNext : IFluentSyntax
    {
        return createTableColumnAsTypeSyntax.AsCustom("TEXT");
    }

    public static TNext AsMySqlMediumText<TNext>(this IColumnTypeSyntax<TNext> createTableColumnAsTypeSyntax)
        where TNext : IFluentSyntax
    {
        return createTableColumnAsTypeSyntax.AsCustom("MEDIUMTEXT");
    }

    public static TNext AsMySqlVarchar<TNext>(
        this IColumnTypeSyntax<TNext> columnTypeSyntax,
        Int16 max
    )
        where TNext : IFluentSyntax
    {
        var varcharType = $"VARCHAR({max}) COLLATE utf8mb4_bin";
        return columnTypeSyntax.AsCustom(varcharType);
    }

    public static TNext AsMySqlTimestamp<TNext>(this IColumnTypeSyntax<TNext> createTableColumnAsTypeSyntax)
        where TNext : IFluentSyntax
    {
        return createTableColumnAsTypeSyntax.AsCustom("TIMESTAMP");
    }

    public static void DropViewIfExist(
        this IExecuteExpressionRoot execute,
        string tableName
    )
    {
        execute.Sql($"DROP VIEW IF EXISTS {tableName}");
    }

    public static void DropTableIfExists(
        this IExecuteExpressionRoot self,
        string tableName
    )
    {
        self.Sql($"DROP TABLE IF EXISTS {tableName}");
    }

    public static IFluentSyntax CreateTableIfNotExists(
        this MigrationBase self,
        string tableName,
        Func<ICreateTableWithColumnOrSchemaOrDescriptionSyntax, IFluentSyntax> constructTableFunction,
        string schemaName = "dbo"
    )
    {
        if (!self.Schema.Schema(schemaName).Table(tableName).Exists())
        {
            return constructTableFunction(self.Create.Table(tableName));
        }
        else
        {
            return null;
        }
    }

    public static IFluentSyntax CreateColIfNotExists(
        this MigrationBase self,
        string tableName,
        string colName,
        Func<IAlterTableColumnAsTypeSyntax, IFluentSyntax> constructColFunction,
        string schemaName = "dbo"
    )
    {
        if (!self.Schema.Schema(schemaName).Table(tableName).Column(colName).Exists())
        {
            return constructColFunction(self.Alter.Table(tableName).AddColumn(colName));
        }

        return null;
    }

}
