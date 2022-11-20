using System.Text.Json;
using System.Xml.Linq;
using Serilog;

namespace WinTenDev.ZiziTools.Cli.Tools;

/*
 * Reference: https://github.com/TAGC/dotnet-setversion/blob/develop/src/dotnet-setversion/Program.cs
 * Copyright (c) ThymineC
 */

public static class ProjectTool
{
    internal const int ExitSuccess = 0;
    internal const int ExitFailure = 1;
    internal static bool UseVersionPrefix = false;

    public static void UpdateProjectVersion(string mode)
    {
        var buildProps = "Directory.Build.props";
        var baseDirectory = Directory.GetCurrentDirectory();

        var majorNumber = DateTime.UtcNow.Year.ToString().Replace("0", "");
        var minorNumber = DateTime.UtcNow.Month;
        var buildNumber = VersionUtil.GetBuildNumber();
        var revNumber = VersionUtil.GetRevNumber();
        var projectVersion = $"{majorNumber}.{minorNumber}.{buildNumber}.{revNumber}";

        Environment.SetEnvironmentVariable("VERSION_NUMBER", projectVersion);

        switch (mode)
        {
            case "DependsOnCondition":
                if (File.Exists(buildProps))
                {
                    Log.Information("Updating {BuildProps}...", buildProps);
                    SetVersion(projectVersion, buildProps);
                }
                else
                {
                    RunRecursive(baseDirectory: baseDirectory, version: projectVersion);
                }
                break;
            case "RootOnly":
                Log.Information("Updating {BuildProps}...", buildProps);
                SetVersion(projectVersion, buildProps);
                break;
            case "RootAndAllProjects":
                Log.Information("Updating {BuildProps}...", buildProps);

                SetVersion(projectVersion, buildProps);
                RunRecursive(baseDirectory: baseDirectory, version: projectVersion);
                break;
            case "AllProjectsOnly":
                RunRecursive(baseDirectory: baseDirectory, version: projectVersion);
                break;
            default:
                break;
        }

        var envVersionNumber = Environment.GetEnvironmentVariable("VERSION_NUMBER");
        Log.Information("Project version updated to {ProjectVersion}", projectVersion);
        Log.Information("Environment variable VERSION_NUMBER set to {EnvVersionNumber}", envVersionNumber);
    }

    private static int RunRecursive(
        string baseDirectory,
        string version
    )
    {
        var csprojFiles = GetCsprojFiles(baseDirectory, recursive: true);
        if (!CheckCsprojFiles(csprojFiles, allowMultiple: true))
            return ExitFailure;

        if (ShouldExtractVersionFromFile(version, out var versionFile) &&
            !TryExtractVersionFromFile(versionFile, out version))
        {
            return ExitFailure;
        }

        foreach (var csprojFile in csprojFiles)
        {
            SetVersion(version, csprojFile);
        }

        PrintSuccessString(version, csprojFiles);
        return ExitSuccess;
    }

    private static string[] GetCsprojFiles(
        string baseDirectory,
        bool recursive
    )
    {
        var projectFiles = Directory
            .EnumerateFileSystemEntries(
                path: baseDirectory,
                searchPattern: "*.csproj",
                searchOption: recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            ).ToArray();

        Log.Information("Found {Count} project files", projectFiles.Length);

        return projectFiles;
    }

    private static bool CheckCsprojFiles(
        string[] csprojFiles,
        bool allowMultiple
    )
    {
        if (csprojFiles.Length == 0)
        {
            Console.WriteLine("Specify a project file. The current working directory does not contain a project file.");
            return false;
        }

        if (!allowMultiple &&
            csprojFiles.Length > 1)
        {
            Console.WriteLine("Specify which project file to use because this folder contains more than one project file.");
            return false;
        }

        return true;
    }

    private static bool ShouldExtractVersionFromFile(
        string version,
        out string versionFile
    )
    {
        if (version.StartsWith("@"))
        {
            versionFile = version.Substring(1);
            return true;
        }

        versionFile = null;
        return false;
    }

    private static bool TryExtractVersionFromFile(
        string filename,
        out string version
    )
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($"The specified file to extract the version from was not found: {filename}");
            version = null;
            return false;
        }

        var versionFileText = File.ReadAllText(filename).Trim();

        try
        {
            var versionModel = JsonSerializer.Deserialize<VersionModel>(versionFileText);
            version = versionModel.ToString();
        }
        catch (JsonException)
        {
            // Simple Version Number
            version = versionFileText;
        }

        return true;
    }

    private static void SetVersion(
        string version,
        string csprojFile
    )
    {
        if (version == null) throw new ArgumentNullException(nameof(version));
        if (csprojFile == null) throw new ArgumentNullException(nameof(csprojFile));

        var versionElement = UseVersionPrefix ? "VersionPrefix" : "Version";

        var document = XDocument.Load(csprojFile);
        var projectNode = document.GetOrCreateElement("Project");
        var versionNode = projectNode
                              .Elements("PropertyGroup")
                              .SelectMany(it => it.Elements(versionElement))
                              .SingleOrDefault() ??
                          projectNode
                              .GetOrCreateElement("PropertyGroup")
                              .GetOrCreateElement(versionElement);
        versionNode.SetValue(version);
        File.WriteAllText(csprojFile, document.ToString());
    }

    private static void PrintSuccessString(
        string version,
        string file
    )
    {
        Console.WriteLine($"Set version to {version} in {file}");
    }

    private static void PrintSuccessString(
        string version,
        params string[] files
    )
    {
        Log.Information("Set version to {Version} in:", version);

        foreach (var file in files)
        {
            Log.Information("==> {File}", file);
        }
    }
}