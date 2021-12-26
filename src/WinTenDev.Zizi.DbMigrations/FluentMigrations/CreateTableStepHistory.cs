using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120211217224501)]
[UsedImplicitly]
public class CreateTableStepHistory : Migration
{

    private const string TableName = "step_histories";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt32().Identity().PrimaryKey()
            .WithColumn("name").AsMySqlText()
            .WithColumn("first_name").AsMySqlText()
            .WithColumn("last_name").AsMySqlText()
            .WithColumn("reason").AsString()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("user_id").AsInt64()
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