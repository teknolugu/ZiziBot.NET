using System.Collections.Generic;
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
    public List<string>? Descriptions { get; set; }
    public List<string>? Warnings { get; set; }
    public List<string>? Notes { get; set; }
    public List<List<ButtonMarkup>>? Buttons { get; set; }
}