using Serilog;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.ZiziBot.AppHost.Handlers;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.BlackLists;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Chat;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.GlobalBan;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Metrics;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Notes;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.ShalatTime;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.SpamLearning;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;
using WinTenDev.ZiziBot.AppHost.Handlers.Commands.Words;

namespace WinTenDev.ZiziBot.AppHost.Extensions;

public static class CommandBuilderExtension
{
    public static IBotBuilder ConfigureBot()
    {
        Log.Information("Building commands..");

        return new BotBuilder()
                .Use<ExceptionHandler>()
                .UseWhen<WebhookLogger>(When.WebHook)
                // .Use<CheckChatPhotoHandler>()
                .Use<NewUpdateHandler>()
                // .Use<CustomUpdateLogger>()
                //.UseWhen<UpdateMembersList>(When.MembersChanged)
                .UseWhen<NewChatMembersHandler>(When.NewChatMembers)
                .UseWhen<LeftChatMemberHandler>(When.LeftChatMember)

                //.UseWhen(When.MembersChanged, memberChanged => memberChanged
                //    .UseWhen(When.MembersChanged, cmdBranch => cmdBranch
                //        .Use<NewChatMembersCommand>()
                //        )
                //    )
                .UseWhen<PinnedMessageHandler>(When.NewPinnedMessage)
                // .UseWhen<MediaReceivedHandler>(When.MediaReceived)
                .UseWhen(
                    When.NewOrEditedMessage,
                    msgBranch => msgBranch
                        .UseWhen(
                            When.CallTagReceived,
                            tagBranch => tagBranch
                                .Use<FindTagCommand>()
                        )
                        .UseWhen(
                            When.NewTextMessage,
                            txtBranch => txtBranch
                                .UseWhen<PingHandler>(When.PingReceived)
                                .UseWhen(
                                    When.NewCommand,
                                    cmdBranch => cmdBranch
                                        .UseCommand<AboutCommand>("about")
                                        .UseCommand<AddBlockListCommand>("addblist")
                                        .UseCommand<AddKataCommand>("kata")
                                        .UseCommand<AddKataCommand>("wfil")
                                        .UseCommand<AddNoteCommand>("addfilter")
                                        .UseCommand<AdminCommand>("admin")
                                        .UseCommand<AdminCommand>("adminlist")
                                        .UseCommand<AfkCommand>("afk")
                                        .UseCommand<AllDebridCommand>("ad")
                                        .UseCommand<BackupDbCommand>("backup_db")
                                        .UseCommand<BanCommand>("ban")
                                        .UseCommand<BotCommand>("bot")
                                        .UseCommand<CatCommand>("cat")
                                        .UseCommand<CatCommand>("cats")
                                        .UseCommand<CheckResiCommand>("resi")
                                        .UseCommand<CovidCommand>("covid")
                                        .UseCommand<DebugCommand>("dbg")
                                        .UseCommand<DeleteBanCommand>("ungban")
                                        .UseCommand<DeleteBanCommand>("dban")
                                        .UseCommand<DeleteKataCommand>("dkata")
                                        .UseCommand<DeleteKataCommand>("delkata")
                                        .UseCommand<DelCityCommand>("del_city")
                                        .UseCommand<DelRssCommand>("delrss")
                                        .UseCommand<DemoteCommand>("demote")
                                        .UseCommand<EpicFreeGamesCommand>("egs_free")
                                        .UseCommand<ExportRssCommand>("exportrss")
                                        .UseCommand<FireCommand>("fire")
                                        .UseCommand<GBanRegisterCommand>("gbanreg")
                                        .UseCommand<GlobalBanCommand>("fban")
                                        .UseCommand<GlobalBanCommand>("gban")
                                        .UseCommand<GlobalReportCommand>("greport")
                                        .UseCommand<GlobalBanSyncCommand>("gbansync")
                                        .UseCommand<HelpCommand>("help")
                                        .UseCommand<IdCommand>("id")
                                        .UseCommand<AllCommand>("all")
                                        .UseCommand<ImportGBanCommand>("import_gban")
                                        .UseCommand<ImportLearnCommand>("importlearn")
                                        .UseCommand<ImportRssCommand>("importrss")
                                        .UseCommand<KataSyncCommand>("ksync")
                                        .UseCommand<KickCommand>("kick")
                                        .UseCommand<LearnCommand>("learn")
                                        .UseCommand<MediaFilterCommand>("mfil")
                                        .UseCommand<NudityCommand>("nudity")
                                        .UseCommand<OcrCommand>("ocr")
                                        .UseCommand<OutCommand>("out")
                                        .UseCommand<PinCommand>("pin")
                                        .UseCommand<PredictCommand>("predict")
                                        .UseCommand<PromoteCommand>("promote")
                                        .UseCommand<QrCommand>("qr")
                                        .UseCommand<RandomCommand>("ran")
                                        .UseCommand<ReportCommand>("report")
                                        .UseCommand<ResetSettingsCommand>("rsettings")
                                        .UseCommand<RestrictCommand>("mute")
                                        .UseCommand<RssCtlCommand>("rssctl")
                                        .UseCommand<RssStartCommand>("startrss")
                                        .UseCommand<RssStopCommand>("stoprss")
                                        .UseCommand<RssInfoCommand>("rssinfo")
                                        .UseCommand<RssPullCommand>("rsspull")
                                        .UseCommand<RulesCommand>("rules")
                                        .UseCommand<SetCityCommand>("set_city")
                                        .UseCommand<SetRulesCommand>("setrules")
                                        .UseCommand<SetRssCommand>("addrss")
                                        .UseCommand<SetRssCommand>("setrss")
                                        .UseCommand<SettingsCommand>("settings")
                                        .UseCommand<SetCommand>("set")
                                        .UseCommand<SetWelcomeCommand>("setwelcome")
                                        .UseCommand<SetWelcomeCommand>("set_welcome_btn")
                                        .UseCommand<SetWelcomeCommand>("set_welcome_doc")
                                        .UseCommand<SetWelcomeCommand>("set_welcome_msg")
                                        .UseCommand<ShalatTimeCommand>("shalat")
                                        .UseCommand<StartCommand>("start")
                                        .UseCommand<StatsCommand>("stats")
                                        .UseCommand<StickerPackCommand>("stickerpack")
                                        .UseCommand<StorageCommand>("storage")
                                        .UseCommand<TagCommand>("retag")
                                        .UseCommand<TagCommand>("tag")
                                        .UseCommand<TagsCommand>("notes")
                                        .UseCommand<TagsCommand>("tags")
                                        .UseCommand<TestCommand>("test")
                                        .UseCommand<TranslateCommand>("tr")
                                        .UseCommand<ReadQrCommand>("read_qr")
                                        .UseCommand<ReadQrCommand>("readqr")
                                        .UseCommand<UntagCommand>("untag")
                                        .UseCommand<UsernameCommand>("username")
                                        .UseCommand<WarnCommand>("warn")
                                        .UseCommand<WelcomeButtonCommand>("welbtn")
                                        .UseCommand<WelcomeCommand>("welcome")
                                        .UseCommand<WelcomeDocumentCommand>("weldoc")
                                        .UseCommand<WelcomeMessageCommand>("welmsg")
                                    // .UseCommand<WgetCommand>("wget")
                                    // .UseCommand<PingCommand>("ping")
                                )
                            //.Use<NLP>()
                        )
                        // .UseWhen<StickerHandler>(When.StickerMessage)
                        .UseWhen<WeatherReporter>(When.LocationMessage)
                )
                .UseWhen<CallbackQueryHandler>(When.CallbackQuery)

            //.Use<UnhandledUpdateReporter>()
            ;
    }
}
