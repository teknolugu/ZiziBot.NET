using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Maintenance(MigrationStage.AfterAll)]
[UsedImplicitly]
public class CreateViewChatAdmin : Migration
{
    private const string TableName = "view_chat_admin";

    public override void Up()
    {
        const string sql = $@"create or replace view {TableName} as 
                            select
                            chat_admin.id, 
                            user_id,
                            role,
                            chat_admin.chat_id,
                            chat_title,
                            chat_type,
                            members_count,
                            is_admin,
                            settings.created_at,
                            updated_at
                                from chat_admin
                                inner join group_settings settings
                                on chat_admin.chat_id = settings.chat_id
                            where chat_type != 'Channel'
                            order by created_at;";

        Execute.Sql(sql);

    }

    public override void Down()
    {
        Execute.DropViewIfExist(TableName);
    }
}
