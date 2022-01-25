using FluentMigrator;
using JetBrains.Annotations;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200705202114)]
[UsedImplicitly]
public class CreateTableAfk : Migration
{
    private const string TableName = "afk";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("afk_reason").AsString().Nullable()
            .WithColumn("is_afk").AsBoolean().WithDefaultValue(0)
            .WithColumn("afk_start").AsDateTime().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("afk_end").AsDateTime().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}