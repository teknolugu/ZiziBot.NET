using BotFramework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.Zizi.DbMigrations.Extensions;
using WinTenDev.Zizi.Hangfire;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils.Extensions;
using WinTenDev.ZiziBot.Alpha1.Extensions;
using WinTenDev.ZiziBot.Alpha1.Handlers.Tags;

namespace WinTenDev.ZiziBot.Alpha1
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMappingConfiguration();

            services.AddTelegramBot();
            services.AddTelegramBotRawUpdateParser<CloudTag, TagParser>();
            services.AddWtTelegramApi();

            services.AddFluentMigration();
            services.AddSqlKataMysql();
            services.AddLiteDb();

            services.AddCacheTower();

            services.AddCommonService();
            services.AddCommandHandlers();

            services.AddHangfireServerAndConfig();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env
        )
        {
            app.UseSerilogRequestLogging();

            app.ConfigureNewtonsoftJson();
            app.ConfigureDapper();

            app.UseHangfireDashboardAndServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(
                endpoints => {
                    endpoints.MapGet(
                        "/",
                        async context =>
                            await context.Response.WriteAsync("Hello World!")
                    );
                }
            );
        }
    }
}