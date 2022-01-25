using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200603201728)]
[UsedImplicitly]
public class CreateTableTags : Migration
{
    private const string TableName = "tags";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("tag").AsString()
            .WithColumn("content").AsMySqlText()
            .WithColumn("btn_data").AsMySqlText()
            .WithColumn("type_data").AsString().WithDefaultValue(-1)
            .WithColumn("file_id").AsString()
            .WithColumn("from_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}