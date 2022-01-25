using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120201028201115)]
[UsedImplicitly]
public class CreateTableWordFilters : Migration
{
    private const string TableName = "word_filter";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("word").AsString()
            .WithColumn("is_global").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("from_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}