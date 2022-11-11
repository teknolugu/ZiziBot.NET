using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SerilogTimings;

namespace WinTenDev.ZiziTools.Cli;

public static class Cmd
{
    public static CommandLineBuilder CreateCommandLineBuilder()
    {
        var root = new RootCommand("help -h")
        {
            new Option<string>("--toolName", "Tool name")
        };

        root.Handler = CommandHandler.Create<ToolOptions, IHost>(Run);
        return new CommandLineBuilder(root);
    }

    static void Run(
        ToolOptions options,
        IHost host
    )
    {
        var serviceProvider = host.Services;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(Program));

        logger.LogInformation("Welcome to Zizi Tools CLI");

        var toolName = options.ToolName;
        logger.LogInformation("Tool Options: {@Options}", options);

        var op = Operation.Begin("Running tool Name: {ToolName}", toolName);

        switch (toolName)
        {
            case "UpdateVersionRoot":
                ProjectTool.UpdateProjectVersion();
                break;
            default:
                logger.LogWarning("Tool name not found!");
                break;
        }

        op.Complete();
    }
}