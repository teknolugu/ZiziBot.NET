namespace WinTenDev.Zizi.Models.Types;

public class StringAnalyzer
{
    public int WordsCount { get; set; }
    public int UpperStrCount { get; set; }
    public int LowerStrCount { get; set; }
    public int AllStrCount { get; set; }
    public int AlphaNumStrCount { get; set; }
    public int AlphaNumStrNoSpaceCount { get; set; }
    public double FireRatio { get; set; }
    public string ResultNote { get; set; }
    public bool IsFired { get; set; }
}