// Contains .NET 6-related Cake targets

var ext = IsRunningOnWindows() ? ".exe" : "";
var dotnetPath = $"./bin/dotnet/dotnet{ext}";

Task("dotnet")
    .Description("Provisions .NET 6 into bin/dotnet based on eng/Versions.props")
    .Does(() =>
    {
        var binlog = $"artifacts/dotnet-{configuration}.binlog";
        var settings = new DotNetCoreBuildSettings
        {
            MSBuildSettings = new DotNetCoreMSBuildSettings()
                .EnableBinaryLogger(binlog)
                .SetConfiguration(configuration),
        };
        DotNetCoreBuild("./src/DotNet/DotNet.csproj");
    });

Task("dotnet-pack")
    .Description("Build and create .NET 6 NuGet packages")
    .IsDependentOn("dotnet")
    .IsDependentOn("dotnet-buildtasks")
    .Does(()=>
    {
        
    });

Task("dotnet-buildtasks")
    .IsDependentOn("DotNet")
    .Does(() =>
    {
        RunMSBuildWithLocalDotNet("./Microsoft.Maui.BuildTasks-net6.sln");
    });

Task("VS-NET6")
    .Description("Provisions .NET 6 and launches an instance of Visual Studio using it.")
    .IsDependentOn("dotnet")
    .IsDependentOn("dotnet-buildtasks")
    .Does(() =>
    {
        StartVisualStudioForDotNet6();
    });

Task("VS-WINUI")
    .Description("Provisions .NET 6 and launches an instance of Visual Studio with WinUI projects.")
    .IsDependentOn("dotnet")
    .IsDependentOn("dotnet-buildtasks")
    .Does(() =>
    {
        RunMSBuildWithLocalDotNet("./Microsoft.Maui.WinUI.sln");
        StartVisualStudioForDotNet6("./Microsoft.Maui.WinUI.sln");
    });

string FindMSBuild()
{
    if (IsRunningOnWindows())
    {
        var vsInstallation = VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild", IncludePrerelease = true });
        if (vsInstallation != null)
        {
            var path = vsInstallation.CombineWithFilePath(@"MSBuild\Current\Bin\MSBuild.exe");
            if (FileExists(path))
                return path.FullPath;
            
            path = vsInstallation.CombineWithFilePath(@"MSBuild\15.0\Bin\MSBuild.exe");
            if (FileExists(path))
                return path.FullPath;
        }
    }
    return "msbuild";
}

void SetDotNetEnvironmentVariables()
{
    var dotnet = MakeAbsolute(Directory("./bin/dotnet/")).ToString();
    var target = EnvironmentVariableTarget.Process;
    Environment.SetEnvironmentVariable("DOTNET_INSTALL_DIR", dotnet, target);
    Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnet, target);
    Environment.SetEnvironmentVariable("DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR", dotnet, target);
    Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0", target);
    Environment.SetEnvironmentVariable("MSBuildEnableWorkloadResolver", "true", target);
    Environment.SetEnvironmentVariable("PATH", dotnet + System.IO.Path.PathSeparator + EnvironmentVariable("PATH"), target);
}

void StartVisualStudioForDotNet6(string sln = "./Microsoft.Maui-net6.sln")
{
    if (isCIBuild)
    {
        Information("This target should not run on CI.");
        return;
    }
    if (!IsRunningOnWindows())
    {
        Information("This target is only supported on Windows.");
        return;
    }

    var vsLatest = VSWhereLatest(new VSWhereLatestSettings { IncludePrerelease = true, });
    if (vsLatest == null)
        throw new Exception("Unable to find Visual Studio!");
    SetDotNetEnvironmentVariables();
    Environment.SetEnvironmentVariable("_ExcludeMauiProjectCapability", "true", EnvironmentVariableTarget.Process);
    StartProcess(vsLatest.CombineWithFilePath("./Common7/IDE/devenv.exe"), sln);
}

// NOTE: this method works as long as the DotNet target has already run
void RunMSBuildWithLocalDotNet(string sln)
{
    var name = System.IO.Path.GetFileNameWithoutExtension(sln);
    var binlog = $"artifacts/{name}-{configuration}.binlog";

    // If we're not on Windows, just use ./bin/dotnet/dotnet, that's it
    if (!IsRunningOnWindows ())
    {
        DotNetCoreBuild(sln,
            new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                ToolPath = dotnetPath,
                MSBuildSettings = new DotNetCoreMSBuildSettings
                {
                    BinaryLogger = new MSBuildBinaryLoggerSettings
                    {
                        Enabled = true,
                        FileName = binlog,
                    },
                },
            });
        return;
    }

    // Otherwise we need to set env variables and run MSBuild
    SetDotNetEnvironmentVariables();
    MSBuild(sln,
        new MSBuildSettings
        {
            Configuration = configuration,
            BinaryLogger = new MSBuildBinaryLogSettings
            {
                Enabled  = true,
                FileName = binlog,
            },
            ToolPath = FindMSBuild(),
        }
        .WithRestore());
}