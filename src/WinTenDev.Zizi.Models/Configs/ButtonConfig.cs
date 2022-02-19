using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Configs;

public class ButtonConfig
{
    public List<ButtonItem>? Items { get; set; }
}

public class ButtonItem
{
    public string Key { get; set; }
    public ButtonData Data { get; set; }
}

public class ButtonData
{
    public List<ButtonCaption>? Captions { get; set; }
    public List<List<ButtonMarkup>>? Buttons { get; set; }
}

public class ButtonCaption
{
    public BotEnvironmentLevel MinimumLevel { get; set; }
    public IEnumerable<string> Sections { get; set; }
}

public class ButtonParsed
{
    public bool IsEnabled { get; set; }
    public bool NextHandler { get; set; }
    public List<string> AllowsAt { get; set; }
    public List<string> ExceptsAt { get; set; }
    public string Caption { get; set; }
    public InlineKeyboardMarkup Markup { get; set; }
}