using FluentMigrator;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200603212809)]
public class CreateTableWarnHistory : Migration
{
    private const string TableName = "warn_history";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("first_name").AsMySqlVarchar(250)
            .WithColumn("last_name").AsMySqlVarchar(250)
            .WithColumn("from_id").AsInt32()
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