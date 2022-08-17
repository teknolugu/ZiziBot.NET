using Microsoft.Extensions.DependencyInjection;

namespace WinTenDev.Zizi.Extensions;

public static class LibrarySetupExtension
{
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(
            expression => {
                expression.AddMaps("WinTenDev.Zizi.Models");
            }
        );

        return services;
    }

}