using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200603212809)]
[UsedImplicitly]
public class CreateTableWarnHistory : Migration
{
    private const string TableName = "warn_history";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("first_name").AsString()
            .WithColumn("last_name").AsString()
            .WithColumn("from_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
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