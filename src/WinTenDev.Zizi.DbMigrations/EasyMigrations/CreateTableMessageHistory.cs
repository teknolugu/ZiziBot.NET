using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.EasyMigrations;

[Migration(120220228112801)]
[UsedImplicitly]
public class CreateTableMessageHistory : Migration
{

    private const string TableName = "message_history";

    public override void Up()
    {
        if (Schema.Table(TableName).Exists()) return;

        Create.Table(TableName)
            .WithIdColumn()
            .WithColumn("message_flag").AsString().NotNullable()
            .WithColumn("message_id").AsInt64().NotNullable()
            .WithColumn("from_id").AsInt64().NotNullable()
            .WithColumn("chat_id").AsInt64().NotNullable()
            .WithColumn("delete_at").AsDateTime().NotNullable()
            .WithTimeStamps();
    }

    public override void Down()
    {
        Delete.Table(TableName);
    }
}