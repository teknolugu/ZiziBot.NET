using System.ComponentModel;

namespace WinTenDev.Zizi.Models.Enums;

public enum StepHistoryName
{
    UnknownFeature = -1,

    [Description("Poto")]
    ChatMemberPhoto,

    [Description("Username")]
    ChatMemberUsername,

    HumanVerification
}

public enum StepHistoryStatus
{
    NeedVerify,
    HasVerify,
    HasKicked,
    ActionDone
}