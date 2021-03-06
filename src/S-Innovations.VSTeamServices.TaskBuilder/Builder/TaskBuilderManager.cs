﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.Models;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.Builder
{

    class CamelCase : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member,
            MemberSerialization memberSerialization)
        {
            var res = base.CreateProperty(member, memberSerialization);

            var attrs = member
                .GetCustomAttributes(typeof(JsonPropertyAttribute), true);
            if (attrs.Any())
            {
                var attr = (attrs[0] as JsonPropertyAttribute);
                if (res.PropertyName != null)
                    res.PropertyName = attr.PropertyName;
            }

            return res;
        }
    }

    public class TaskBuilder
    {
     
        public static void BuildSelf()
        {
            var assembly = Assembly.GetEntryAssembly();
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            var pathToExe = Path.Combine(Path.GetDirectoryName(path), System.AppDomain.CurrentDomain.FriendlyName);

            BuildFromAssembly(assembly, pathToExe);
        }

        public static async Task PublishSelf(string[] args)
        {
            var assembly = Assembly.GetEntryAssembly();
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path1 = Uri.UnescapeDataString(uri.Path);
            string folder = Path.GetDirectoryName(path1);
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {

                foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    var entry = zip.CreateEntry(file.Substring(folder.Length + 1));
                    using (var ws = entry.Open())
                    {
                        using (var fs = File.OpenRead(file))
                        {
                            await fs.CopyToAsync(ws);
                        }
                    }

                }
            }

           var bytes= ms.ToArray();
           


            var http = new HttpClient();
            var encodedCred = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + args[2]));

            var taskjson = File.ReadAllText(Path.Combine(folder, "task.json"));
            Console.WriteLine(taskjson);

            var taskId = JToken.Parse(taskjson).SelectToken("$.id").ToString();

            var req = new HttpRequestMessage(HttpMethod.Put, $"{args[1]}/_apis/distributedtask/tasks/{taskId}?api-version=2.0-preview");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedCred);
        //    req.Headers.Add("Content-Range", $"bytes 0-${bytes.Length-1}/${bytes.Length}");

            req.Content = new ByteArrayContent(bytes);
            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            req.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, bytes.Length - 1, bytes.Length);


            var rsp = await http.SendAsync(req);
            if(rsp.StatusCode != System.Net.HttpStatusCode.Created)
            {
                Console.WriteLine(await rsp.Content.ReadAsStringAsync());
            }
   
            
        }
        public static JObject BuildFromAssembly(Assembly assembly,string pathToDll)
        {
         

            var json = new TaskJson();

            json.Name = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            if (!new Regex("^[a-zA-Z0-9]*$").IsMatch(json.Name))
            {
                throw new ArgumentException("AssemblyProductAttribute must be alphnumeric");
            }
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
            var result = TaskHelper.GetTaskInputs(programOptionsType, null);

            json.Inputs = result.Inputs.OrderByDescending(k => k.Order).ToArray();
            json.Groups = result.Groups.ToArray();
            json.SourceDefinitions = result.SourceDefinitions.ToArray();


            json.Execution = new TaskExecution
            {
                ExecutionType = "PowerShell3",
                Target = "$(currentDirectory)\\OauthBroker.ps1",
                WorkingDirectory = "$(currentDirectory)",
            };




            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new CamelCase(),
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
            if (!Directory.Exists(Path.Combine(outputDir, "ps_modules")))
            {
                using (var zip = new ZipArchive(typeof(TaskBuilder).Assembly.GetManifestResourceStream("SInnovations.VSTeamServices.TaskBuilder.ps_modules.zip"), ZipArchiveMode.Read))
                {
                    zip.ExtractToDirectory(Path.Combine(outputDir, "ps_modules"));

                }
            }

            File.WriteAllText(Path.Combine(outputDir, "task.json"), obj.ToString(Newtonsoft.Json.Formatting.Indented));
            return obj;

        }
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

            var obj = BuildFromAssembly(assembly,pathToDll);

            AppDomain.CurrentDomain.AssemblyResolve -= loader;
            return obj;
        }

       

        private const string PSStringType = "String";
        private static string PSType(string type)
        {

            switch (type)
            {
                case "multiLine":
                case "pickList":
                case "boolean":
                case "bool":
                case "string":
                case "connectedService:AzureRM":
                case "connectedService:Generic":
                case "filePath":
                    return PSStringType;
            }
            Console.WriteLine("Warning: Type not known for  PSType " + type);
            return PSStringType;

        }

        private static void WriteOauthBrokerPowershell(StreamWriter writer, string program, TaskInput[] inputs)
        {

            if (!program.EndsWith(".dll"))
                program += ".dll";

            //writer.WriteLine($"[CmdletBinding(DefaultParameterSetName = 'None')]");
            //writer.WriteLine("param");
            //writer.WriteLine("(");
            //foreach (var input in inputs)
            //{
            //    writer.WriteLine($"\t[{PSType(input.Type)}] [Parameter(Mandatory = {(input.Required && string.IsNullOrEmpty(input.VisibleRule) ? "$true" : "$false")})]");
            //    writer.WriteLine($"\t${input.Name}{(input == inputs.Last() ? "" : ",")}");

            //}
            //writer.WriteLine(")");

            writer.WriteLine("[CmdletBinding()]");
            writer.WriteLine("param()");
            writer.WriteLine("Trace-VstsEnteringInvocation $MyInvocation");

            writer.WriteLine("Try\n{ ");

            foreach (var input in inputs)
            {
                writer.WriteLine($"[{(input.Type == "boolean" ?"bool":"string")}]${input.Name} = Get-VstsInput -Name {input.Name} {(input.Required && string.IsNullOrEmpty(input.VisibleRule)? "-Require ":"")}{(input.Type == "boolean"? "-AsBool ":"")}{( input.IsJsonArray ? " | ConvertFrom-Json" : "")}");
            }
          

          //  foreach (var input in inputs.Where(t => t.Type == "boolean"))
           // {
          //      writer.WriteLine($"[bool]${input.Name} = ${input.Name}  -eq 'true'");
           // }
            //  writer.WriteLine(@"$adal = ""${env: ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Services\Microsoft.IdentityModel.Clients.ActiveDirectory.dll""");
            //   writer.WriteLine("[System.Reflection.Assembly]::LoadFrom($adal)");



            foreach (var i in Enumerable.Range(0, inputs.Length))
            {
                var input = inputs[i];
                switch (input.Type)
                {
                    case "multiLine":
                    case "filePath":
                    case "string":
                    case "pickList":
                        if (input.IsJsonArray)
                        {
                            //  $arg4 = '';
                            //  foreach ($element in $DeploymentName) {$arg4 = $arg4 + ' ' + ('"' + $element + '"')}
                            writer.WriteLine($"$arg{i} = '--{input.Name}'");
                            writer.WriteLine($"foreach ($element in $DeploymentName) {{$arg{i} = $arg{i} + ' ' + ('\"' + $element + '\"')}}");
                          //  writer.WriteLine($"$arg{i} =  if ([String]::IsNullOrEmpty(${input.Name}))				{{ '' }} else {{ @('--{input.Name}',		${input.Name})  }}");
                        }
                        else
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
          

            var sb = new StringBuilder();

            foreach (var serviceEndpoint in inputs.Where(i => i.Type.StartsWith("connectedService:")))
            {
                var rng = Path.GetRandomFileName().Substring(0, 5);
                writer.WriteLine($"$serviceEndpoint_{rng} = Get-VstsEndpoint -Name \"${serviceEndpoint.Name}\" -Require");

                if (serviceEndpoint.Type == "connectedService:AzureRM")
                {


                    var a = WritePSVariable(writer, rng, "ServicePrincipalId");
                    var b = WritePSVariable(writer, rng, "ServicePrincipalKey");
                    var c = WritePSVariable(writer, rng, "TenantId");

                    writer.WriteLine($"$azureSubscriptionId_{rng} = $serviceEndpoint_{rng}.Data.SubscriptionId");
                    //   writer.WriteLine($"$azureSubscriptionName_{rng} = $serviceEndpoint_{rng}.Data.SubscriptionName");

                    sb.Append($" --TenantId {c} --SubscriptionId $azureSubscriptionId_{rng} --PrincipalKey {b} --PrincipalId {a}");
                }
                else if (serviceEndpoint.Type == "connectedService:Generic")
                {
                    var prefix = serviceEndpoint.Name;
                    var a = WritePSVariable(writer, rng, "Username");
                    var b = WritePSVariable(writer, rng, "Password");
                    sb.Append($" --{prefix}Username {a} --{prefix}Password {b}");
                }else
                {
                    writer.WriteLine($"$auth_{rng} = ($serviceEndpoint_{rng}.Auth | ConvertTo-Json -Compress) -replace \"\"\"\",\"'\"");
                    sb.Append($" --{serviceEndpoint.Name}Auth \"$auth_{rng}\"");
                }



            }
            writer.WriteLine("$cwd =  Split-Path -parent $PSCommandPath");

            if (program.EndsWith(".exe"))
            {
                writer.WriteLine($"$CMD = \"$cwd/{program}\"");
                writer.Write("& $CMD");
            }
            else
            {
               

                writer.WriteLine($"$CMD = \"dotnet\"");
                writer.Write($"& $CMD $cwd/{program}");
            }

           
            writer.Write(sb.ToString());

           

            writer.WriteLine($" {string.Join(" ", Enumerable.Range(0, inputs.Length).Select(i => $"$arg{i}"))}");

            writer.WriteLine("if ($lastexitcode -ne 0)");
            writer.WriteLine("{");
            writer.WriteLine("\tthrow \"Task Failed\"");
            writer.WriteLine("}");

            writer.WriteLine("}");
            writer.WriteLine("finally\n{");
            writer.WriteLine(" Trace-VstsLeavingInvocation $MyInvocation");
            writer.WriteLine("}");
   

        }

        private static string WritePSVariable(StreamWriter writer, string rng, string a)
        {
            writer.WriteLine($"${a}_{rng} = $serviceEndpoint_{rng}.Auth.Parameters.{a}");
            return $"${a}_{rng}";
        }
    }

    
}
