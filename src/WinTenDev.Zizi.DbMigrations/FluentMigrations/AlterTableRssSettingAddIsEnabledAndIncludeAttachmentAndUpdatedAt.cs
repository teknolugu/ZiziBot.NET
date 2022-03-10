using System;
using FluentMigrator;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220308051201)]
public class AlterTableRssSettingAddIsEnabledAndIncludeAttachmentAndUpdatedAt : Migration
{

    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableRssSettings.TableName,
            "is_enabled",
            syntax =>
                syntax.AsBoolean().WithDefaultValue(1)
        );

        this.CreateColIfNotExists(
            CreateTableRssSettings.TableName,
            "include_attachment",
            syntax =>
                syntax.AsBoolean().WithDefaultValue(0)
        );

        this.CreateColIfNotExists(
            CreateTableRssSettings.TableName,
            "updated_at",
            syntax =>
                syntax.AsDateTime().WithDefaultValue(DateTime.UtcNow)
        );
    }

    public override void Down()
    {
        // nothing action
    }
}