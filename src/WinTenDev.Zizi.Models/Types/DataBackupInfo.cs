namespace WinTenDev.Zizi.Models.Types;

public class DataBackupInfo
{
    public string FileName { get; set; }
    public string FullName { get; set; }
    public string FileSizeSql { get; set; }
    public string FileNameZip { get; set; }
    public long FileSizeSqlRaw { get; set; }
    public string FileSizeSqlZip { get; set; }
    public long FileSizeSqlZipRaw { get; set; }
    public string FullNameZip { get; set; }
}