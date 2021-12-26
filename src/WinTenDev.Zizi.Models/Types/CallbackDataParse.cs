using System.Collections.Generic;

namespace WinTenDev.Zizi.Models.Types;

public class CallbackDataParse
{
    public string CallbackData { get; set; }
    public List<string> CallbackDataSplit { get; set; }
    public string CallbackDataCmd { get; set; }
    public string CallbackArgStr { get; set; }
    public List<string> CallbackArgs { get; set; }
}