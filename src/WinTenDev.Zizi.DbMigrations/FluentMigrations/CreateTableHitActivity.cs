using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200815154101)]
[UsedImplicitly]
public class CreateTableHitActivity : Migration
{
    private const string TableName = "hit_activity";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("via_bot").AsString().Nullable()
            .WithColumn("message_type").AsString().Nullable()
            .WithColumn("from_id").AsInt64()
            .WithColumn("from_first_name").AsMySqlText().Nullable()
            .WithColumn("from_last_name").AsMySqlText().Nullable()
            .WithColumn("from_username").AsString().Nullable()
            .WithColumn("from_lang_code").AsString()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("chat_username").AsString().Nullable()
            .WithColumn("chat_type").AsString()
            .WithColumn("chat_title").AsString().Nullable()
            .WithColumn("timestamp").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}