using FluentMigrator;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120200530065305)]
public class CreateTableWordsLearning : Migration
{
    private const string TableName = "words_learning";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("label").AsMySqlVarchar(20)
            .WithColumn("message").AsMySqlText()
            .WithColumn("from_id").AsInt32()
            .WithColumn("chat_id").AsInt64()
            .WithColumn("timestamp").AsMySqlTimestamp().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}