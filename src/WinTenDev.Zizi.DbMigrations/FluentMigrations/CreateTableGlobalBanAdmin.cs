using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200603195441)]
[UsedImplicitly]
public class CreateTableGlobalBanAdmin : Migration
{
    private const string TableName = "gban_admin";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt64()
            .WithColumn("username").AsString()
            .WithColumn("promoted_by").AsInt64()
            .WithColumn("promoted_from").AsInt64()
            .WithColumn("is_banned").AsBoolean()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}