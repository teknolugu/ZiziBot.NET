namespace WinTenDev.Zizi.Models.Enums;

public enum ChatTypeEx
{
    Private,

    /// <summary>
    /// Normal groupchat
    /// </summary>
    Group,

    /// <summary>
    /// A channel
    /// </summary>
    Channel,

    /// <summary>
    /// A supergroup
    /// </summary>
    SuperGroup,

    /// <summary>
    /// “sender” for a private chat with the inline query sender
    /// </summary>
    Sender,

    Null,

    Unknown
}