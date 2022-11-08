using Newtonsoft.Json;
using RepoDb.Interfaces;
using RepoDb.Options;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.RepoDb;

public class UpdatePropertyHandler : IPropertyHandler<string, Update>
{
    public Update Get(
        string input,
        PropertyHandlerGetOptions options
    )
    {
        return JsonConvert.DeserializeObject<Update>(input);
    }
    public string Set(
        Update input,
        PropertyHandlerSetOptions options
    )
    {
        return JsonConvert.SerializeObject(input);
    }
}