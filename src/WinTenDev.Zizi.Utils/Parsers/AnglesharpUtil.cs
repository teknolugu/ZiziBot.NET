using AngleSharp;

namespace WinTenDev.Zizi.Utils.Parsers;

public static class AnglesharpUtil
{
    public static IBrowsingContext DefaultContext
    {
        get
        {
            var config = Configuration.Default.WithDefaultLoader().WithJs().WithCss();
            var context = BrowsingContext.New(config);

            return context;
        }
    }
}
