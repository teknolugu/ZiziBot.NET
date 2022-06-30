using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Humanizer;
using Humanizer.Localisation;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils;

public static class AppHostUtil
{
	public static string GetAppHostInfo(
		bool includeUptime = false,
		bool includePath = false
	)
	{
		var hostName = Dns.GetHostName();
		var processUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
		var assemblyName = Assembly.GetEntryAssembly().GetName();
		var version = assemblyName.Version.ToString();

		var htmlMessage = HtmlMessage.Empty
			.Bold("Host Information").Br()
			.Bold("Name: ").CodeBr(hostName)
			.Bold("OS: ").CodeBr(Environment.OSVersion.Platform.ToString())
			.Bold("Version: ").CodeBr(Environment.OSVersion.Version.ToString())
			.Bold("Uptime: ").CodeBr(TimeSpan.FromMilliseconds(Environment.TickCount64).Humanize(precision: 10, minUnit: TimeUnit.Second))
			.Bold("Runtime: ").CodeBr(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
			.Bold("DateTime: ").CodeBr(DateTime.Now.ToDetailDateTimeString())
			.Br()
			.Bold("App Information").Br()
			.Bold("Name: ").CodeBr(assemblyName.Name)
			.Bold("Version: ").Code(version).Br()
			.Bold("Environment: ").CodeBr(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

		if (includeUptime)
		{
			htmlMessage
				.Bold("Uptime: ").CodeBr(TimeSpan.FromMilliseconds(processUptime.TotalMilliseconds).Humanize(precision: 10, minUnit: TimeUnit.Second));
		}

		if (includePath)
		{
			htmlMessage
				.Bold("Path: ").CodeBr(Environment.ProcessPath)
				.Bold("Directory: ").CodeBr(Environment.CurrentDirectory);
		}

		return htmlMessage.ToString();
	}
}
