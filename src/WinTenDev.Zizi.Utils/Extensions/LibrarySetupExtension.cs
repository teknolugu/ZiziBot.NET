using Dapper;
using Dapper.FluentMap;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class LibrarySetupExtension
{

    public static IApplicationBuilder ConfigureNewtonsoftJson(this IApplicationBuilder app)
    {
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            ContractResolver = contractResolver,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        return app;
    }

    public static IApplicationBuilder ConfigureDapper(this IApplicationBuilder app)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        FluentMapper.Initialize(configuration => {
            configuration.AddMap(new ChatSettingMap());
        });

        return app;
    }
}