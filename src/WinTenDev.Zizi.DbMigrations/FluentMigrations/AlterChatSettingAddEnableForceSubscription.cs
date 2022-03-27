using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220320235601)]
[UsedImplicitly]
public class AlterChatSettingsAddEnableForceSubscription : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableChatSettings.TableName,
            "enable_force_subscription",
            syntax => syntax
                .AsBoolean()
                .WithDefaultValue(0)
        );
    }

    public override void Down()
    {
        // noting
    }
}