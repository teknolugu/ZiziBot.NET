using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220221222701)]
[UsedImplicitly]
public class AlterChatSettingAddFireCheck : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "enable_fire_check",
            syntax => syntax
                .AsBoolean()
                .WithDefaultValue(0)
        );
    }

    public override void Down()
    {
        // Nothing to do here
    }
}
