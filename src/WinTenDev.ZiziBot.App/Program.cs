using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.Zizi.Utils.Extensions;

namespace WinTenDev.ZiziBot.App
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception exception)
            {
                // Log.Fatal(exception, "Hosting failure!");
                Console.WriteLine("Hosting failure!");
                Console.WriteLine(exception);
            }
            finally
            {
                Log.Information("Close and Flush serilog!");
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((hostBuilderContext, serviceProvider, loggerConfiguration) => {
                    loggerConfiguration.AddSerilogBootstrapper(serviceProvider);
                })
                .ConfigureAppConfiguration((context, builder) => {
                    builder.AddJsonFile("appsettings.json", true, true);
                })
                .ConfigureServices((context, services) => {
                    services.MappingAppSettings();
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((context, builder) => {
                    builder.AddSerilog();
                });
        }
    }
}