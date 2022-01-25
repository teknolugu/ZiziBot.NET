using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120201028113701)]
[UsedImplicitly]
public class CreateTableHumanVerification : Migration
{
    private const string TableName = "human_verification";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().Identity().PrimaryKey()
            .WithColumn("chat_id").AsInt64().NotNullable()
            .WithColumn("from_id").AsInt64().NotNullable()
            .WithColumn("created_at").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}