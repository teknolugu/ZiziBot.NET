using FluentMigrator;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200314172001)]
public class CreateTableRssSettings : Migration
{
    private const string TableName = "rss_settings";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("from_id").AsInt32().NotNullable()
            .WithColumn("chat_id").AsInt64().NotNullable()
            .WithColumn("url_feed").AsMySqlText().NotNullable()
            .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}