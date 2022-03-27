using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Models.Types
{
    public class FileMetaData
    {
        public string FileId { get; set; }
        public string UniqueFileId { get; set; }
        public MessageType Type { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
    }
}
