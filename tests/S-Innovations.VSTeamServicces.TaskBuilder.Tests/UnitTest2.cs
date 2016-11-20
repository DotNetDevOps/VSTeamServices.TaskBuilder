using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Builder;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using VSTeamServicesTaskGenerator;
using System.Linq;

namespace TaskInstallerTests
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void TestMethod1()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../tasks/VSTeamServicesTaskGenerator/bin/Debug/net452/win7-x64/VSTeamServicesTaskGenerator.exe");
            if (File.Exists(file))
            {
                Console.WriteLine(TaskBuilder.BuildTask(file).ToString(Newtonsoft.Json.Formatting.Indented));

            }
        }

        [TestMethod]
        public void TestGlob()
        {
            var glob = new GlobPath(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory() , "../../../../")).FullName + "**/bin/**/*.exe");
            var files = glob.MatchedFiles();
            foreach(var file in files)
            {
                Console.WriteLine(file);
            }
        }

        //[TestMethod]
        //public void TestProgram()
        //{
        //    Program.Main(new string[] {"--Path" , @"C:\dev\sinnovations\S-Innovations.VSTeamServices.Tasks\src\CreateOrUpdateServiceBusTask\bin\Debug\CreateOrUpdateServiceBusTask.exe" });




        //}

        //[TestMethod]
        //public void TestProgram2()
        //{
        //    Program.Main(new string[] { "--Path", @"C:\dev\sinnovations\S-Innovations.VSTeamServices\tasks\UpdateNugetPackageVersionsTask\bin\Debug\UpdateNugetPackageVersionsTask.exe" });




        //}

        [TestMethod]
        public void TestProgram1()
        {
            //  Program.Main(new string[] { });


            var glob = new GlobPath("C:/dev/blobtest space/*.txt");
            var files = glob.MatchedFiles();
            foreach (var file in files)
            {
                Console.WriteLine(file);
            }

        }
        [TestMethod]
        public void TestAuthKey()
        {
            var b = new PropertyRelation<ResourceGroupOptions, string>(k => k.ResourceGroup);
            var s = b.GetAuthKey();
        }


        [TestMethod]
        public void TestVariableSearch()
        {
            var value = "[concat('Data Source=tcp:', reference(concat('Microsoft.Sql/servers/', variables('dbServerName'))).fullyQualifiedDomainName, ',1433;Initial Catalog=', parameters('databaseName'), ';User Id=', parameters('dbAdministratorLogin'), '@', variables('dbServerName'), ';Password=', parameters('dbAdministratorLoginPassword'), ';')]";

            var variables = Regex.Matches(value, @"variables\('(.*?)'\)").OfType<Match>().Select(m=>m.Groups[1].Value).Distinct().ToArray();


        }

        [TestMethod]
        public void TestParentFolderSearch()
        {
            var Pattern = @"C:\dev\wildlife-copy\artifacts\**\project.json\..\wwwroot\**\*";
            var glob = new GlobPath(Pattern);

            var files = glob.MatchedFiles().ToArray();
        }
        [TestMethod]
        public void TestRootFinding()
        {
            var Pattern = @"C:\output\**\*";
            var glob = new GlobPath(Pattern);

            foreach(var file in glob.MatchedFiles())
            {
              
          
                Console.WriteLine(file);
            }
            var globtest = new Glob(glob.Pattern);
            var a = globtest.IsMatch(@"C:/output/ApplicationManifest.xml");
        }
    }
}
