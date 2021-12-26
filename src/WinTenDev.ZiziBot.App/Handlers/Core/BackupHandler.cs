using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.ZiziBot.App.Handlers.Core
{
    /// <summary>
    /// This class is contains about Backup Command handler
    /// </summary>
    public class BackupHandler : ZiziEventHandler
    {
        private readonly ILogger<BackupHandler> _logger;
        private readonly DataBackupService _dataBackupService;
        private readonly PrivilegeService _privilegeService;

        /// <summary>
        /// Constructor for BackupHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dataBackupService"></param>
        /// <param name="privilegeService"></param>
        public BackupHandler
        (
            ILogger<BackupHandler> logger,
            DataBackupService dataBackupService,
            PrivilegeService privilegeService
        )
        {
            _logger = logger;
            _dataBackupService = dataBackupService;
            _privilegeService = privilegeService;
        }

        /// <summary>
        /// This method is used to handle /backup command
        /// </summary>
        [Command("backup", CommandParseMode.Both)]
        public async Task CmdBackup()
        {
            var op = Operation.Begin("Backup Command Handler");

            await DeleteMessageAsync(Message.MessageId);

            if (!_privilegeService.IsFromSudo(FromId))
            {
                _logger.LogInformation("Backup Data only for Sudo!");
                op.Complete();

                return;
            }

            await SendMessageTextAsync("Memulai mencadangkan Data..");

            var dataBackupInfo = await _dataBackupService.BackupMySqlDatabase();
            var fileName = dataBackupInfo.FileName;
            var fullName = dataBackupInfo.FullName.GetDirectory();
            var fullNameZip = dataBackupInfo.FullNameZip;
            var fileSize = dataBackupInfo.FileSizeSqlZipRaw.SizeFormat();
            var fileSizeRaw = dataBackupInfo.FileSizeSqlRaw.SizeFormat();

            var htmlMessage = new HtmlString()
                .Bold("Name: ").Code(fileName).Br()
                .Bold("Path: ").Code(fullName).Br()
                .Bold("Size: ").Code(fileSize).Br()
                .Bold("RawSize: ").Code(fileSizeRaw);

            await DeleteMessageAsync();
            await SendMediaDocumentAsync(fullNameZip, MediaType.LocalDocument, htmlMessage);

            op.Complete();
        }
    }
}