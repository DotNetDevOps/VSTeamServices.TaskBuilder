using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

[assembly: Guid("D9BAFED4-AB18-4F58-968D-86655B4D2CE9")]
[assembly: AssemblyTitle("Updating Nuget Packages")]
[assembly: AssemblyDescription("Updating Nuget Packages")]
[assembly: AssemblyInformationalVersion("1.0.30")]  //Update to do new release
[assembly: AssemblyConfiguration("Utility")]
[assembly: AssemblyCompany("S-Innovations v/Poul K. Sørensen")]
[assembly: AssemblyProduct("UpdateNugetPackageVersionsTask")]

namespace UpdateNugetPackageVersionsTask
{
    [EntryPoint("Updating Nuget Packages")]
    public class UpdateNugetPackageVersionsTaskOptions
    {
        //[Display(GroupName = "Feed", Description = "The Nuget Feed to publish packages to after update", Name = "Nuget Feed")]
        //[Option("MyGetPackageFeed")]
        //public string MyGetPackageFeed { get; set; }

        //[Display(GroupName = "Feed", Description = "The Nuget Feed key", Name = "Nuget Key")]
        //[Option("MyGetPackageFeedKey")]
        //public string MyGetPackageFeedKey { get; set; }

        //[Display(GroupName = "Feed", Description = "The Nuget Feed key", Name = "Nuget Key")]
        [Option("PackageFolder",Default ="$(Build.StagingDirectory)")]
        public string PackageFolder { get; set; }

      
        [Option("PackageFeatureName", Default ="pre")]
        public string PackageFeatureName { get; set; }

    }
    class Program
    {
        static void Main(string[] args)
        {

            

            var options = ConsoleHelper.ParseAndHandleArguments<UpdateNugetPackageVersionsTaskOptions>("Running Nuget Package Version Updates", args);

            var nugets = Directory.GetFiles(options.PackageFolder, "*.nupkg");

            Console.WriteLine(String.Join(", ", nugets));

            string appendversion = $"{options.PackageFeatureName}-" + Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER").Split('_').Last().Replace(".", "");//context.GetValue(this.PrereleaseName);
            var idx = appendversion.LastIndexOf('-');
            var rev = appendversion.Substring(idx + 7).PadLeft(2, '0');
            appendversion = string.Format("{0}{1}{2}", appendversion.Substring(0, idx), appendversion.Substring(idx, 7), rev);
            Console.WriteLine(appendversion);
            var nugetpath = string.Format(@"{0}\Agent\Worker\Tools\nuget.exe", Environment.GetEnvironmentVariable("AGENT_HOMEDIRECTORY"));


            foreach (var path in nugets)
            {

                //var othernugets = context.GetValue(this.OtherNugets).Select(f => Path.GetFileName(f));


                var othernugets = nugets.Where(a => a != path).Select(f => Path.GetFileName(f));
                Console.WriteLine(String.Join(", ", othernugets));
                // // context.TrackBuildWarning("New Execute", BuildMessageImportance.High);
                //// context.TrackBuildWarning(appendversion, BuildMessageImportance.High);
                //// context.TrackBuildWarning(string.Join("\n", othernugets), BuildMessageImportance.High);
                using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Update))
                {

                    XDocument doc;
                    //   XNamespace ns = "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd";
                    //  XNamespace nscore = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd";
                    var entry = zip.Entries.FirstOrDefault(f => f.Name.EndsWith(".nuspec"));
                    if (entry == null)
                        Console.WriteLine("ENTRY WAS NULL");

                    var name = entry.Name;
                    using (var xmlstream = entry.Open())
                    {

                        doc = XDocument.Load(xmlstream);
                        Console.WriteLine(doc.ToString());

                        var metadata = doc.Root.Elements().First(e => e.Name.LocalName == "metadata");
                        var version = metadata.Elements().First(e => e.Name.LocalName == "version");

                        if (version.Value.EndsWith(appendversion.Split('-').First()))
                            version.Value += "-" + string.Join("", appendversion.Split('-').Skip(1));
                        else
                            version.Value += "-" + appendversion;

                        var dependencies = metadata.Elements().First(e => e.Name.LocalName == "dependencies");


                        Console.WriteLine((string.Join("\n", dependencies.Descendants().Where(n => n.Name.LocalName == "dependency").Select(e =>
                             string.Format("{0}.{1}.nupkg", e.Attribute("id").Value, e.Attribute("version").Value)))));


                        foreach (var dependency in dependencies.Descendants().Where(n => n.Name.LocalName == "dependency").Where(e => IncludedInBuild(e, othernugets)))
                        {
                            var dependencyElement = othernugets.FirstOrDefault(s => s.StartsWith(dependency.Attribute("id").Value) &&
                    char.IsNumber(Path.GetFileNameWithoutExtension(s.Substring(dependency.Attribute("id").Value.Length)).Replace(".", "").First()));

                            var otherversion = Path.GetFileNameWithoutExtension(dependencyElement.Substring(dependency.Attribute("id").Value.Length)).Trim('.');
                            var attr = dependency.Attribute("version");

                            if (otherversion.Split('-').Last().Equals(appendversion.Split('-').First()))
                                attr.Value = otherversion + "-" + string.Join("-", appendversion.Split('-').Skip(1));
                            else
                                attr.Value = otherversion + "-" + appendversion;

                        }

                    }

                    Console.WriteLine((doc.ToString()));
                    entry.Delete();
                    entry = zip.CreateEntry(name);
                    using (var xmlstream = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Indent = true, NewLineOnAttributes = false }))
                    {
                        doc.WriteTo(xmlstream);
                    }
                }

                //if (!string.IsNullOrEmpty(options.MyGetPackageFeed))
                //{
                //    var start = new ProcessStartInfo(nugetpath, String.Format(@"push ""{0}"" {1} -Source {2}", path,
                //        options.MyGetPackageFeedKey, options.MyGetPackageFeed));
                //    Console.WriteLine(start.Arguments);
                //    var p = Process.Start(start);
                //    p.WaitForExit();
                //}

            }
        }

        private static bool IncludedInBuild(XElement e, IEnumerable<string> othernugets)
        {

            return othernugets.Any(s => s.StartsWith(e.Attribute("id").Value) &&
                Char.IsNumber(Path.GetFileNameWithoutExtension(s.Substring(e.Attribute("id").Value.Length)).Replace(".", "").First()));

        }
    }
}
