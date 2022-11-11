// See https://aka.ms/new-console-template for more information

using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Serilog;
await Cmd.CreateCommandLineBuilder()
    .UseHost(x => Host.CreateDefaultBuilder(),
        configureHost: hostBuilder => {
            hostBuilder
                .UseSerilog(
                    (
                        context,
                        provider,
                        logger
                    ) => logger.AddSerilogBootstrapper(provider)
                )
                .ConfigureServices(services => {
                    // services.AddSingleton<IGreeter, Greeter>();
                });
        })
    .UseDefaults()
    .Build()
    .InvokeAsync(args);