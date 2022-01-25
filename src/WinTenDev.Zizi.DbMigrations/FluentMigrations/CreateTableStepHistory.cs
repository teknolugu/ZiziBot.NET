using FluentMigrator;
using JetBrains.Annotations;

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
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("name").AsString()
            .WithColumn("first_name").AsString()
            .WithColumn("last_name").AsString()
            .WithColumn("reason").AsString()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("user_id").AsInt64()
            .WithColumn("status").AsString()
            .WithColumn("hangfire_job_id").AsString()
            .WithColumn("last_warn_message_id").AsInt64()
            .WithColumn("created_at").AsDateTime().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}