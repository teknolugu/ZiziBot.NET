using System.Collections.Generic;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class MemberUtil
{
    public static string GetNameLink(this long userId, string name)
    {
        return $"<a href='tg://user?id={userId}'>{name}</a>";
    }

    public static string GetNameLink(this User user)
    {
        var fullName = user.GetFullName();

        return $"<a href='tg://user?id={user.Id}'>{fullName}</a>";
    }

    public static string GetMention(this long userId)
    {
        return userId.GetNameLink("&#8203;");
    }

    public static string GetFullName(this User user)
    {
        var firstName = user.FirstName;
        var lastName = user.LastName;

        return (firstName + " " + lastName).Trim();
    }

    public static string GetFromNameLink(this Message message)
    {
        var fromId = message.From.Id;
        var fullName = message.From.GetFullName();

        return $"<a href='tg://user?id={fromId}'>{fullName}</a>";
    }

    #region Random Name

    public static List<string> GetRandomNames()
    {
        var listStr = new List<string>()
        {
            "fulan",
            "fulanah"
        };

        return listStr;
    }

    public static string GetRandomName()
    {
        return GetRandomNames().RandomElement();
    }

    public static string GetRandomFullName()
    {
        var randomChild = GetRandomName();
        var bin = randomChild == "fulan" ? "bin" : "binti";

        return $"{randomChild} {bin} fulan";
    }

    #endregion
}