using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

[assembly: AssemblyInformationalVersion("1.0.15")]
[assembly: AssemblyTitle("VisualStudio TeamServices Task Generator")]
[assembly: AssemblyDescription("Generate Visual Studio Team Services Tasks using S-Innovations Task Library")]
[assembly: AssemblyCompany("S-Innovations v/Poul Kjeldager Sørensen")]
[assembly: AssemblyProduct("VSTeamServicesTaskGenerator")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyConfiguration("Package")]
[assembly: AssemblyCulture("")]


namespace VSTeamServicesTaskGenerator
{

    [EntryPoint("VSTS Task CLI")]
    public class ProgramOptions
    {
        [Display(Description = "The path to the generated task", Name = "Task Path", ShortName = "Path", ResourceType =typeof(GlobPath))]
        public GlobPath Paths { get; set; }

        [Option("Arguments", Default = "--build")]
        public string Arguments { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Generating Task", args);

            foreach (var path in options.Paths.MatchedFiles())
            {
                ProcessStartInfo ps = null ;
                if (path.EndsWith(".exe"))
                {
                    ps = new ProcessStartInfo(path, options.Arguments);

                }
                else if(path.EndsWith(".dll"))
                {
                    ps = new ProcessStartInfo("dotnet", $"{path} {options.Arguments}");
                }

                ps.UseShellExecute = false;
                var process = Process.Start(ps);
                process.WaitForExit();
                if (process.ExitCode != 0)
                    Environment.Exit(process.ExitCode);
            }

        }
    }
}
