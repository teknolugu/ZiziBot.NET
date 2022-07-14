using System;

namespace WinTenDev.Zizi.Models.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StartupTaskAttribute : Attribute
{
    public bool AfterHostReady { get; set; }
}