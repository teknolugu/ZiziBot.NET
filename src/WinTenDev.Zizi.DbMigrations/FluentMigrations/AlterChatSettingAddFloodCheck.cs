using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220304184001)]
[UsedImplicitly]
public class AlterChatSettingAddFloodCheck : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "enable_flood_check",
            syntax => syntax
                .AsBoolean()
                .WithDefaultValue(0)
        );

        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "flood_offset",
            syntax => syntax
                .AsInt16()
                .WithDefaultValue(7)
        );
    }

    public override void Down()
    {
        // Nothing to do here
    }
}