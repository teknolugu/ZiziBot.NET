using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220306154501)]
[UsedImplicitly]
public class AlterChatSettingAddLangAndTimeZone : Migration
{
    public override void Up()
    {
        this.CreateColIfNotExists(
            tableName: CreateTableChatSettings.TableName,
            colName: "timezone_offset",
            constructColFunction: syntax =>
                syntax.AsString().WithDefaultValue(value: "07:00")
        );

        this.CreateColIfNotExists(
            tableName: CreateTableChatSettings.TableName,
            colName: "language_code",
            constructColFunction: syntax =>
                syntax.AsString().WithDefaultValue(value: "id")
        );
    }

    public override void Down()
    {
        // Nothing to do here
    }
}