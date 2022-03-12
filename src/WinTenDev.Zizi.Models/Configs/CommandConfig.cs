using System.Collections.Generic;

namespace WinTenDev.Zizi.Models.Configs;

public class CommandConfig
{
    public bool EnsureOnStartup { get; set; }
    public List<CommandItem>? CommandItems { get; set; }
}

public class CommandItem
{
    public string Command { get; set; }
    public string Description { get; set; }
}