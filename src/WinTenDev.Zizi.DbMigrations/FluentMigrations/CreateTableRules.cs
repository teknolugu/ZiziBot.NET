using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120220303110401)]
[UsedImplicitly]
public class CreateTableRules : Migration
{
    private const string TableName = "rules";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithIdColumn()
            .WithColumn("from_id").AsInt64()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("rule_text").AsString(4000)
            .WithTimeStamps();
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}