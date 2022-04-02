using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(120220331092401)]
[UsedImplicitly]
public class CreateTableChatAdmin : Migration
{
    private const string TableName = "chat_admin";

    public override void Up()
    {
        this.CreateTableIfNotExists(
            TableName,
            syntax => syntax
                .WithIdColumn()
                .WithColumn("chat_id").AsInt64().NotNullable()
                .WithColumn("user_id").AsInt64().NotNullable()
                .WithColumn("role").AsString().NotNullable()
                .WithTimeStamp()
        );
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}