using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200705201614)]
[UsedImplicitly]
public class CreateTableRssHistory : Migration
{
    private const string TableName = "rss_history";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("rss_source").AsMySqlVarchar(255)
            .WithColumn("url").AsMySqlText()
            .WithColumn("title").AsMySqlVarchar(255)
            .WithColumn("publish_date").AsDateTime()
            .WithColumn("author").AsMySqlVarchar(150)
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}