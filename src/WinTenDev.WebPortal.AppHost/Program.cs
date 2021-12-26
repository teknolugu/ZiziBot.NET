using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.WebPortal.AppHost.Helpers;

namespace WinTenDev.WebPortal.AppHost
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            SerilogHelper.SetupSerilog();

            await CreateHostBuilder(args)
                .Build()
                .RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog();
        }
    }
}