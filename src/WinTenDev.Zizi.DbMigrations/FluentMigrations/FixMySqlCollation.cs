using FluentMigrator;
using JetBrains.Annotations;
using MoreLinq;

namespace WinTenDev.Zizi.DbMigrations.FluentMigrations;

// [Maintenance(MigrationStage.AfterAll)]
[UsedImplicitly]
public class FixMySqlCollation : Migration
{
    public override void Up()
    {
        const string defaultCharSet = "utf8mb4";
        const string defaultCollation = "utf8mb4_unicode_ci";

        IfDatabase("MySql")
            .Execute.Sql
            (
                "ALTER DATABASE " +
                $"CHARACTER SET {defaultCharSet} " +
                $"COLLATE {defaultCollation};"
            );

        new[]
        {
            "afk",
            "filters",
            "gban_admin",
            "global_bans",
            "group_settings",
            "hit_activity",
            "human_verification",
            "rss_history",
            "rss_settings",
            "safe_members",
            "spells",
            "step_histories",
            "tags",
            "warn_history",
            "warn_username_history",
            "words_learning",
            "word_filter"
        }.ForEach
        (
            (tableName) => {
                IfDatabase("MySql")
                    .Execute.Sql
                    (
                        $"ALTER TABLE {tableName} " +
                        $"CONVERT TO CHARACTER " +
                        $"SET {defaultCharSet} " +
                        $"COLLATE {defaultCollation};"
                    );
            }
        );

    }
    public override void Down()
    {
        // None
    }
}