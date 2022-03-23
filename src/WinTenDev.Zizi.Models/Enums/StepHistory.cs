using System.ComponentModel;

namespace WinTenDev.Zizi.Models.Enums;

public enum StepHistoryName
{
    UnknownFeature = -1,

    [Description("Photo")]
    ChatMemberPhoto,

    [Description("Username")]
    ChatMemberUsername,

    HumanVerification,

    ForceSubscription
}

public enum StepHistoryStatus
{
    NeedVerify,
    HasVerify,
    HasKicked,
    ActionDone
}