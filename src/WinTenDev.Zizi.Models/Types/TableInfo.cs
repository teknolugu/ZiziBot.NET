using System;

namespace WinTenDev.Zizi.Models.Types;

public class TableInfo
{
    public string TableSchema { get; set; }
    public string TableName { get; set; }
    public string TableCollation { get; set; }
    public long TableRows { get; set; }
    public long DataLength { get; set; }
    public long AutoIncrement { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}