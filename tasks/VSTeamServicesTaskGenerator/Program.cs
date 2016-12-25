﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.Builder;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;

[assembly: AssemblyInformationalVersion("1.0.14")]
[assembly: AssemblyTitle("VisualStudio TeamServices Task Generator")]
[assembly: AssemblyDescription("Generate Visual Studio Team Services Tasks using S-Innovations Task Library")]
[assembly: AssemblyCompany("S-Innovations v/Poul Kjeldager Sørensen")]
[assembly: AssemblyProduct("VSTeamServicesTaskGenerator")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyConfiguration("Package")]
[assembly: AssemblyCulture("")]


namespace VSTeamServicesTaskGenerator
{

    [EntryPoint("Creating VSTS Task")]
    public class ProgramOptions
    {
        [Display(Description = "The path to the generated task", Name = "Task Path", ShortName = "Path", ResourceType =typeof(GlobPath))]
        public GlobPath Paths { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Generating Task", args);

            foreach (var path in options.Paths.MatchedFiles())
            {

                //TaskBuilder.BuildTask(path);
                var ps = new ProcessStartInfo(path, "--build");
              

                ps.UseShellExecute = false;
               // ps.RedirectStandardOutput = true;
               // ps.RedirectStandardError = true;
                var process = Process.Start(ps);
               // process.OutputDataReceived += (sender, args1) => Console.WriteLine("received output: {0}", args1.Data);
               // process.BeginOutputReadLine();

                //process.ErrorDataReceived += (sender, args1) => Console.WriteLine("received err: {0}", args1.Data);
                //process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    Environment.Exit(process.ExitCode);
            }

        }
    }
}