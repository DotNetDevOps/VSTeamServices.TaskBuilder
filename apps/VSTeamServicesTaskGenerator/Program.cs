using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.Builder;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyTitle("VisualStudio TeamServices Task Generator")]
[assembly: AssemblyDescription("Generate Visual Studio Team Services Tasks using S-Innovations Task Library")]
[assembly: AssemblyCompany("S-Innovations /v Poul Kjeldager Sørensen")]
[assembly: AssemblyProduct("VSTeamServicesTaskGenerator")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyConfiguration("Package")]
[assembly: AssemblyCulture("")]


namespace VSTeamServicesTaskGenerator
{

    [EntryPoint("Creating VSTS Task")]
    public class ProgramOptions
    {
        [Display(Description = "The path to the generated task", Name = "Task Path")]
        [Option("Path")]
        public string Path { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Generating Task", args);


            TaskBuilder.BuildTask(options.Path);

        }
    }
}
