using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120201028233957)]
[UsedImplicitly]
public class CreateTableFilters : Migration
{
    private const string TableName = "filters";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("slug").AsString().NotNullable()
            .WithColumn("content").AsMySqlText()
            .WithColumn("btn_data").AsMySqlText()
            .WithColumn("from_id").AsInt64().NotNullable()
            .WithColumn("chat_id").AsInt64().NotNullable()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}