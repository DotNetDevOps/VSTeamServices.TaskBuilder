﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.Builder
{
    internal class TaskBuilder
    {
        public static JObject BuildTask(string pathToDll)
        {
            ResolveEventHandler loader = delegate (object source, ResolveEventArgs e)
            {
                var name = e.Name.Split(',').First();
                if (!name.EndsWith(".dll"))
                    name += ".dll";

                if (File.Exists(Path.Combine(Path.GetDirectoryName(pathToDll), name)))
                {
                    return Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(pathToDll), name));
                }

                throw new DllNotFoundException(e.Name);

            };
            AppDomain.CurrentDomain.AssemblyResolve += loader;
            var assembly = Assembly.LoadFile(pathToDll);


            var json = new TaskJson();

            json.Name = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            json.FriendlyName = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            json.Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            json.Author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            json.Category = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Utility";
            json.Visibility = new[] { "Build", "Release" };
            json.Id = assembly.GetCustomAttribute<GuidAttribute>().Value.ToString();
            json.Demands = new[] { "azureps" };
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('.').Select(i => int.Parse(i)).ToArray();
            json.Version = new Models.Version { Major = version[0], Minor = version[1], Patch = version[2] };
            json.MinimumAgentVersion = "1.92.0";
            json.InstanceNameFormat = "";


            if (json.FriendlyName.Length > 40)
            {
                throw new ArgumentException($"Task Title can only be up to 40chars : Change {json.FriendlyName}");
            }


            var programOptionsType = assembly.DefinedTypes.SingleOrDefault(t => Attribute.IsDefined(t, typeof(EntryPointAttribute)));
            json.InstanceNameFormat = programOptionsType.GetCustomAttribute<EntryPointAttribute>().InstanceFormat;
            var result = TaskHelper.GetTaskInputs(programOptionsType);

            json.Inputs = result.Inputs.OrderByDescending(k => k.Order).ToArray();
            json.Groups = result.Groups;


            json.Execution = new TaskExecution
            {
                ExecutionType = "PowerShell",
                Target = "$(currentDirectory)\\OauthBroker.ps1",
                WorkingDirectory = "$(currentDirectory)",
            };




            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });
            var obj = JObject.FromObject(json, serializer);
            Console.WriteLine(obj.ToString(Newtonsoft.Json.Formatting.Indented));

            var outputDir = Path.GetDirectoryName(pathToDll);
            using (var writer = new StreamWriter(File.Open(Path.Combine(outputDir, "OauthBroker.ps1"), FileMode.Create)))
            {
                WriteOauthBrokerPowershell(writer, Path.GetFileName(pathToDll), json.Inputs);
                writer.Flush();
            }
            File.WriteAllText(Path.Combine(outputDir, "task.json"), obj.ToString(Newtonsoft.Json.Formatting.Indented));


            AppDomain.CurrentDomain.AssemblyResolve -= loader;
            return obj;
        }
        private const string PSStringType = "String";
        private static string PSType(string type)
        {

            switch (type)
            {
                case "pickList":
                case "boolean":
                case "bool":
                case "string":
                case "connectedService:AzureRM":
                case "filePath":
                    return PSStringType;
            }

            throw new NotImplementedException(type);
        }

        private static void WriteOauthBrokerPowershell(StreamWriter writer, string program, TaskInput[] inputs)
        {
            writer.WriteLine($"[CmdletBinding(DefaultParameterSetName = 'None')]");
            writer.WriteLine("param");
            writer.WriteLine("(");
            foreach (var input in inputs)
            {
                writer.WriteLine($"\t[{PSType(input.Type)}] [Parameter(Mandatory = {(input.Required ? "$true" : "$false")})]");
                writer.WriteLine($"\t${input.Name}{(input == inputs.Last() ? "" : ",")}");

            }
            writer.WriteLine(")");

            foreach (var input in inputs.Where(t => t.Type == "boolean"))
            {
                writer.WriteLine($"[bool]${input.Name} = ${input.Name}  -eq 'true'");
            }
            //  writer.WriteLine(@"$adal = ""${env: ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Services\Microsoft.IdentityModel.Clients.ActiveDirectory.dll""");
            //   writer.WriteLine("[System.Reflection.Assembly]::LoadFrom($adal)");



            foreach (var i in Enumerable.Range(0, inputs.Length))
            {
                var input = inputs[i];
                switch (input.Type)
                {
                    case "filePath":
                    case "string":
                    case "pickList":
                        writer.WriteLine($"$arg{i} =  if ([String]::IsNullOrEmpty(${input.Name}))				{{ '' }} else {{ @('--{input.Name}',			('\"'+${input.Name}+'\"'))  }}");
                        break;
                    case "boolean":
                        writer.WriteLine($"$arg{i} =  if (!${input.Name})				{{ '' }} else {{ '--{input.Name}'}}");
                        break;
                    default:
                        Console.WriteLine($"{input.Type} was not known for powershell generation.");
                        break;
                }

            }
            writer.WriteLine("$cwd =  Split-Path -parent $PSCommandPath");
            writer.WriteLine($"$CMD = \"$cwd/{program}\"");

            writer.Write("& $CMD");

            foreach (var serviceEndpoint in inputs.Where(i => i.Type.StartsWith("connectedService:")))
            {
                var rng = Path.GetRandomFileName().Substring(0, 5);
                writer.WriteLine($"$serviceEndpoint_{rng} = Get-ServiceEndpoint -Name \"${serviceEndpoint.Name}\" -Context $distributedTaskContext");

                if (serviceEndpoint.Type == "connectedService:AzureRM")
                {


                    var a = WritePSVariable(writer, rng, "ServicePrincipalId");
                    var b = WritePSVariable(writer, rng, "ServicePrincipalKey");
                    var c = WritePSVariable(writer, rng, "TenantId");

                    writer.WriteLine($"$azureSubscriptionId_{rng} = $serviceEndpoint_{rng}.Data.SubscriptionId");
                    //   writer.WriteLine($"$azureSubscriptionName_{rng} = $serviceEndpoint_{rng}.Data.SubscriptionName");

                    writer.Write($" --TenantId {c} --SubscriptionId $azureSubscriptionId_{rng} --PrincipalKey {b} --PrincipalId {a}");
                }
                else if (serviceEndpoint.Type == "connectedService:Generic")
                {
                    var prefix = serviceEndpoint.Name;
                    var a = WritePSVariable(writer, rng, "Username");
                    var b = WritePSVariable(writer, rng, "Password");
                    writer.Write($" --{prefix}Username {a} --{prefix}Password {b}");
                }



            }


            writer.WriteLine($" {string.Join(" ", Enumerable.Range(0, inputs.Length).Select(i => $"$arg{i}"))}");



        }

        private static string WritePSVariable(StreamWriter writer, string rng, string a)
        {
            writer.WriteLine($"${a}_{rng} = $serviceEndpoint_{rng}.Authorization.Parameters.{a}");
            return $"${a}_{rng}";
        }
    }
}
