using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120220403145101)]
[UsedImplicitly]
public class CreateTableShalatTime : Migration
{
    private const string TableName = "shalat_time";

    public override void Up()
    {
        this.CreateTableIfNotExists(
            TableName,
            syntax => syntax
                .WithIdColumn()
                .WithColumn("user_id").AsInt64().NotNullable()
                .WithColumn("chat_id").AsInt64().NotNullable()
                .WithColumn("city_id").AsInt32().NotNullable()
                .WithColumn("city_name").AsString().NotNullable()
                .WithColumn("enable_notification").AsBoolean().NotNullable()
                .WithTimeStamps()
        );
    }

    public override void Down()
    {
        Execute.DropTableIfExists(TableName);
    }
}
