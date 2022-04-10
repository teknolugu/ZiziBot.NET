using Humanizer.Bytes;

namespace WinTenDev.Zizi.Utils;

public static class SizeUtil
{
    public static ByteSize ToeByteSize(this string sizeStr)
    {
        var size = ByteSize.Parse(sizeStr);
        return size;
    }
}
