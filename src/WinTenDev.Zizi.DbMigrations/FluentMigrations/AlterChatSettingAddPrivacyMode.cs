using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220417150901)]
[UsedImplicitly]
public class AlterChatSettingAddPrivacyMode : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "enable_privacy_mode",
            syntax => syntax
                .AsBoolean()
                .WithDefaultValue(0)
        );
    }

    public override void Down()
    {
        // Nothing
    }
}
