using System;
using System.Collections.Generic;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public partial class FeatureConfig
{
    public bool IsEnabled { get; set; }
    public List<Item> Items { get; set; }
}

public partial class Item
{
    public string Key { get; set; }
    public bool IsEnabled { get; set; }
    public UserPrivilegeLevel MinimumPrivilege { get; set; }
    public List<string> AllowsAt { get; set; }
    public List<string> ExceptsAt { get; set; }
    public List<Caption> Captions { get; set; }
    public List<List<Button>> Buttons { get; set; }
}

public partial class Button
{
    public string Text { get; set; }
    public Uri Url { get; set; }
}

public partial class Caption
{
    public BotEnvironmentLevel MinimumLevel { get; set; }
    public List<string> Sections { get; set; }
}