using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120210113104401)]
[UsedImplicitly]
public class CreateTableWarnUsernameHistory : Migration
{
    private const string TableName = "warn_username_history";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("from_id").AsInt64()
            .WithColumn("first_name").AsString()
            .WithColumn("last_name").AsString()
            .WithColumn("step_count").AsInt16()
            .WithColumn("last_warn_message_id").AsInt64()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}