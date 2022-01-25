using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200603195301)]
[UsedImplicitly]
public class CreateTableSpells : Migration
{
    private const string TableName = "spells";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("typo").AsString()
            .WithColumn("fix").AsString()
            .WithColumn("from_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}