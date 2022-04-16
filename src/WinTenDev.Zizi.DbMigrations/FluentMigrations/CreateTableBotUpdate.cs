using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120220416033801)]
[UsedImplicitly]
public class CreateTableBotUpdate : Migration
{
    private const string TableName = "bot_update";

    public override void Up()
    {
        this.CreateTableIfNotExists(
            TableName,
            syntax => syntax
                .WithIdColumn()
                .WithColumn("bot_name").AsString().NotNullable()
                .WithTimeStamp()
                .WithColumn("update").AsMySqlText().NotNullable()
        );
    }

    public override void Down()
    {
        Execute.DropTableIfExists(TableName);
    }
}
