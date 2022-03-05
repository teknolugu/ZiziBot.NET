using FluentMigrator;
using JetBrains.Annotations;
using WinTenDev.Zizi.DbMigrations.Extensions;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

[Maintenance(MigrationStage.AfterAll)]
[UsedImplicitly]
public class CreateViewChatSettings : Migration
{
    private const string ViewListGroups = "view_list_groups";
    private const string ViewListPrivates = "view_list_privates";

    public override void Up()
    {
        const string sqlListGroup = $"create or replace view {ViewListGroups} as " +
                                    "select * from group_settings  " +
                                    "where chat_type = 'group' || chat_type = 'supergroup' || chat_type = 'channel' " +
                                    "order by updated_at desc;";

        Execute.Sql(sqlListGroup);

        const string sqlListPrivate = $"create or replace view {ViewListPrivates} as " +
                                      "select * from group_settings  " +
                                      "where chat_type = 'private' " +
                                      "order by updated_at desc;";

        Execute.Sql(sqlListPrivate);

    }

    public override void Down()
    {
        Execute.DropViewIfExist(ViewListGroups);
        Execute.DropViewIfExist(ViewListPrivates);
    }
}