using System;

namespace WinTenDev.Zizi.Models.Configs;

public class ExceptionlessConfig
{
    public bool IsEnabled { get; set; }
    public string ApiKey { get; set; }
    public Uri ServerUrl { get; set; }
    public string[] Tags { get; set; }
}