using System.Collections.Generic;

namespace WinTenDev.Zizi.Models.Configs;

public class RestrictionConfig
{
    public bool EnableRestriction { get; set; }

    public string[] RestrictionArea { get; set; }

    public List<string> AdminCleanUp { get; set; }

    public List<string> Sudoers { get; set; }

    public List<long> IgnoredIds { get; set; }
}