using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220412181901)]
[UsedImplicitly]
public class AlterChatSettingAddEnableSpellCheck : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "enable_spell_check",
            syntax => syntax
                .AsBoolean()
                .WithDefaultValue(1)
        );
    }

    public override void Down()
    {
        throw new System.NotImplementedException();
    }
}
