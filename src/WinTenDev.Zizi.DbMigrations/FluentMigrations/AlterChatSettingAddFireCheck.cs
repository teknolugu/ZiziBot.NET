using FluentMigrator;
using JetBrains.Annotations;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220221222701)]
[UsedImplicitly]
public class AlterChatSettingAddFireCheck : Migration
{

    public override void Up()
    {
        if (!IfDatabase().Schema.Table(CreateTableChatSettings.TableName).Exists())
            Alter.Table(CreateTableChatSettings.TableName).AddColumn("enable_fire_check").AsBoolean().WithDefaultValue(0);
    }
    public override void Down()
    {
        // Nothing to do here
    }
}