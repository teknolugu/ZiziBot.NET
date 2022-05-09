using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Migration(220220421133501)]
[UsedImplicitly]
public class AlterBotUpdateAddChatIdAndUserId : Migration
{
    public override void Up()
    {
        this.CreateColIfNotExists(
            CreateTableBotUpdate.TableName,
            "chat_id",
            syntax => syntax
                .AsInt64()
                .NotNullable()
        );

        this.CreateColIfNotExists(
            CreateTableBotUpdate.TableName,
            "user_id",
            syntax => syntax
                .AsInt64()
                .NotNullable()
        );
    }

    public override void Down()
    {
        // nothing
    }
}
