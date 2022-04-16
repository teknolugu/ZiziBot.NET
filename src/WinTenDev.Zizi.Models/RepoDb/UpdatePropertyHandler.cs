using Newtonsoft.Json;
using RepoDb;
using RepoDb.Interfaces;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.RepoDb;

public class UpdatePropertyHandler : IPropertyHandler<string, Update>
{
    public Update Get(
        string input,
        ClassProperty property
    )
    {
        return JsonConvert.DeserializeObject<Update>(input);
    }

    public string Set(
        Update input,
        ClassProperty property
    )
    {
        return JsonConvert.SerializeObject(input);
    }
}
