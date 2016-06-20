using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.ResourceManager;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.Extensions;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager
{
    public class SimpleArmTemplateDeployment<T> : ArmTemplateDeployment<T, ResourceSource> where T: ArmTemplateOptions<T>, new()
    {
        public SimpleArmTemplateDeployment() : base(o => o.Source)
        {

        }

        public override JObject LoadTemplate(T options)
        {
            options.OnTemplateLoaded();

            return base.LoadTemplate(options);
        }
    }

    public class ArmTemplateOptions<T> where T :ArmTemplateOptions<T>, new()
    {
        public ResourceSource Source { get; set; }
        public ArmTemplateOptions(string path, Assembly assembly)
        {
            Tags = new Dictionary<string, string>();
            ArmDeployment = new SimpleArmTemplateDeployment<T>();

            Source = new ResourceSource(path, assembly)
            {
                 new ResourceTags(Tags)
            };
        }

        public virtual void OnTemplateLoaded()
        {

        }

        [Display(ResourceType = typeof(SimpleArmTemplateDeployment<>), Order = 0)]        
        public SimpleArmTemplateDeployment<T> ArmDeployment { get; set; }

        [Display(ResourceType = typeof(Tags),
         Description = "Tags, seperate tags with comma and key:value with semicolon.",
         Name = "Resource Tags",
         ShortName = "ResourceTags")]
        public Dictionary<string, string> Tags { get; set; }

    }
    public class ResourceTags : ITemplateAction
    {
        public IDictionary<string, string> Tags { get; set; }
        public ResourceTags(IDictionary<string,string> tags)
        {
            Tags = tags;
        }
        public Task TemplateActionAsync(JObject obj)
        {
             obj.SetTags(Tags);

            return Task.FromResult(0);
        }
    }

    public class ArmTemplateDeployment<TOptions, TResourceSource> : ArmTemplateDeployment<TOptions>
        where TResourceSource : ResourceSource
        where TOptions : class,new()
    {
        public Func<TOptions, TResourceSource> TemplateProvider { get; private set; }

        public ArmTemplateDeployment(
            Func<TOptions,TResourceSource> templateProvider,
            Func<TOptions, ServiceEndpoint> endPointProvider = null) : base(endPointProvider)
        {
            TemplateProvider = templateProvider;
        }

        public override JObject LoadTemplate(TOptions options)
        {
            return TemplateProvider(options);
        }
        //public override JObject LoadTemplateParameters()
        //{
        //    return LoadTemplate(null).SelectToken("parameters") as JObject ?? new JObject();
        //}
    }

    public abstract class ArmTemplateDeployment<T> : IConsoleExecutor<T>, ITaskInputFactory, IConsoleReader<T>
          where T : class,new()
    {
        public Func<T, ServiceEndpoint> EndpointProvider { get; private set; }

        public ArmTemplateDeployment(Func<T, ServiceEndpoint> endPointProvider = null)
        {
            ResourceGroupOptions = new ResourceGroupOptions();
            EndpointProvider = endPointProvider ?? ((o) => ResourceGroupOptions.ConnectedServiceName);
           
        }
        public JObject Output { get; set; }
        public JObject Parameters { get; set; }
        public JObject Variables { get;  set; }
        public JObject Template { get; set; }

        public ResourceGroupOptions ResourceGroupOptions { get; set; }
        public JObject OutVariables { get; private set; }



        //  public abstract JObject LoadTemplateParameters();
        //   public abstract JObject LoadTemplateVariables();
        public abstract JObject LoadTemplate(T options);

        public JProperty CreateParameter(string name)
        {
            return new JProperty(name, new JObject(new JProperty("type", "string")));
        }
        public void OnConsoleParsing(Parser parser, string[] args, T options, PropertyInfo info)
        {

            if (!ConsoleHelper.ParseAndHandleArguments(parser, args, ResourceGroupOptions))
            {
                Console.WriteLine($"Failed to read deploymentObj: {JObject.FromObject(ResourceGroupOptions).ToString(Formatting.Indented)} ");
                return;
            }
            var template = LoadTemplate(options);
            var parametersObj = ParamterTypeGenerator.CreateFromParameters(template.SelectToken("parameters") as JObject ?? new JObject());
            if (!parser.ParseArguments(args, parametersObj))
            {
                Console.WriteLine($"Failed to read parameterObj: {JObject.FromObject(parametersObj).ToString(Formatting.Indented)} ");
                return;
            }

            Parameters = new JObject(
                    JObject.FromObject(parametersObj).Properties()
                        .Where(p => p.Value.Type != JTokenType.Null)
                        .Select(p => ResourceManagerHelper.CreateValue(p.Name, p.Value)
                ));

            var prefix = "var";
            var variablesObj = ParamterTypeGenerator.CreateFromVariables(template.SelectToken("variables") as JObject ?? new JObject(),prefix);
            if (!parser.ParseArguments(args, variablesObj))
            {
                Console.WriteLine($"Failed to read variablesObj: {JObject.FromObject(variablesObj).ToString(Formatting.Indented)} ");
                return;
            }
            Variables = JObject.FromObject(variablesObj,JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            var outprefix = "out";
            var outputVariablesObj = ParamterTypeGenerator.CreateFromOutputs(template.SelectToken("outputs") as JObject ?? new JObject(), outprefix);
            if (!parser.ParseArguments(args, outputVariablesObj))
            {
                Console.WriteLine($"Failed to read outputVariablesObj: {JObject.FromObject(variablesObj).ToString(Formatting.Indented)} ");
                return;
            }

            OutVariables = JObject.FromObject(outputVariablesObj);
        }

        public string GetOutputValue(string name)
        {
            return Output.SelectToken($"{name}.value").ToString();

        }
           
        public virtual TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, PropertyInfo parent)
        {
            
            var optionValues = this.GetType().GetCustomAttributes<AllowedValueOptionAttribute>().ToLookup(k => k.ParameterName);
            var template = LoadTemplate(new T());
            var inputs = template.SelectToken("parameters").OfType<JProperty>().Select(t =>
            {
                var obj = t.Value as JObject;
                var variableName = t.Name;

                var allowedValues = obj.SelectToken("allowedValues")?.ToObject<string[]>() ?? new string[] { };
                JObject options = null;
                if (allowedValues.Any())
                {
                    options = new JObject(allowedValues.Select(k => 
                                        new JProperty(k,  
                                            (optionValues.Contains(variableName) && optionValues[variableName].Any(v=>v.OptionName== k) ?
                                                    optionValues[variableName].Single(v => v.OptionName == k).OptionValue :
                                                    k
                                            ).Humanize(LetterCasing.Title))));
                }
                
                return new TaskInput
                {
                    Name = variableName,
                    DefaultValue = obj.SelectToken("defaultValue")?.Value<string>(),
                    GroupName = groupName,
                    HelpMarkDown = obj.SelectToken("metadata.description")?.Value<string>(),
                    Type = allowedValues.Any() ? "pickList" : GetTaskType(obj),
                    Label = t.Name.Humanize(LetterCasing.Title),
                    Required = string.IsNullOrEmpty(obj.SelectToken("defaultValue")?.Value<string>()),
                    Options = options
                };

            }).ToArray();

           
          

            var result = TaskHelper.GetTaskInputs(typeof(ResourceGroupOptions),parent);
            result.Inputs.AddRange(inputs);

            var attrs = parent?.GetCustomAttributes<ParameterSourceDefinitionAttribute>().ToArray() ?? new ParameterSourceDefinitionAttribute[0];
            foreach (var sd in attrs)
            {
                var authKeyProvier = sd.ConnectedService ?? parent.GetCustomAttribute<SourceDefinitionAttribute>()?.ConnectedService;

                    result.SourceDefinitions.Add(new SourceDefinition
                    {
                        Endpoint = sd.Endpoint,
                        AuthKey = authKeyProvier==null?null:(Activator.CreateInstance(authKeyProvier) as AuthKeyProvider)?.GetAuthKey(),
                        Selector = sd.Selector,
                        KeySelector = sd.KeySelector ?? "",
                        Target = sd.ParameterName
                    });
                
            }

            var variables = template.SelectToken("variables")?.OfType<JProperty>().Select(t =>
              new TaskInput()
              {
                  Name = $"var{t.Name}",
                  DefaultValue = t.Value.ToString(),
                  GroupName = "ArmVars",
                  Label = t.Name,
                  Type = "string",
              }
            ).ToArray();

            if (variables?.Any() ?? false)
            {
                result.Inputs.AddRange(variables);
                result.Groups.Add(new Group
                {
                     DisplayName = "Arm Template Variables",
                     Name ="ArmVars", IsExpanded = false,
                });
            }

            var outputs = template.SelectToken("outputs")?.OfType<JProperty>().Select(t =>
              new TaskInput()
              {
                  Name = $"out{t.Name}",
                  DefaultValue = t.Name,
                  GroupName = "ArmOutputVariables",
                  Label = t.Name,
                  Type = "string",
              }
            ).ToArray();
            if (outputs?.Any() ?? false)
            {
                result.Inputs.AddRange(outputs);
                result.Groups.Add(new Group
                {
                    DisplayName = "Copy ARM template outputs to variables",
                    Name = "ArmOutputVariables",
                    IsExpanded = false,
                });
            }


            return result;

        }

        private static string GetTaskType(JObject obj)
        {
            var type = obj.SelectToken("type")?.Value<string>();
            switch (type)
            {
                case "bool":
                    return "boolean";
                case "int":
                case "securestring": //Would be cool with this type in vsts also
                    return "string";
                case "string":
                case "pickList":
                    return type;
            }

            throw new ArgumentException($"{type} not known");

        }
        public List<Action<JObject>> AfterLoad = new List<Action<JObject>>();
         
        public virtual void Execute(T options)
        {

            Template= LoadTemplate(options);

            foreach(var variable in Variables.Properties())
            {
                if(variable.Value != null&&variable.Value.Type != JTokenType.Null)
                    Template.SelectToken($"variables.{variable.Name}").Replace(variable.Value);
            }
            foreach(var action in AfterLoad)
            {
                action(Template);
            }


            if (ResourceGroupOptions.CreateTemplatesOnly)
            {

                return;
            }

            Console.WriteLine("Deploying Resource Group: ");

            Console.WriteLine("\tTemplate:");
            Console.WriteLine("\t\t" + Template.ToString(Formatting.Indented).Replace("\n", "\n\t\t").TrimEnd('\t'));

            Console.WriteLine("\tParameters:");
            Console.WriteLine("\t\t"+Parameters.ToString(Formatting.Indented).Replace("\n","\n\t\t").TrimEnd('\t'));



            var endpoint = EndpointProvider(options);
            var managemenetToken = endpoint.GetToken("https://management.azure.com/");

            if (ResourceGroupOptions.CreateResourceGroup)
            {
                Console.WriteLine($"Creating or Updating Resource Group: {ResourceGroupOptions.ResourceGroup}, {ResourceGroupOptions.ResourceGroupLocation}");
                var rg = ResourceManagerHelper.CreateResourceGroupIfNotExistAsync(
                    endpoint.SubscriptionId, managemenetToken,
                    ResourceGroupOptions.ResourceGroup,
                    ResourceGroupOptions.ResourceGroupLocation).GetAwaiter().GetResult();

                if (rg.Tags == null)
                    rg.Tags = new Dictionary<string,string>();

                if (rg.Tags.MergeChangedReversed(ResourceGroupOptions.Tags))
                {
                    rg = ResourceManagerHelper.UpdateResourceGroupAsync(endpoint.SubscriptionId, managemenetToken, rg)
                        .GetAwaiter().GetResult();
                }
                Console.WriteLine("\tResourceGroup Tags:");
                Console.WriteLine($"{string.Join(" ", rg.Tags.Select(k => $"\t\t[{k.Key}:{k.Value}]\n"))}");


            }

            var result = ResourceManagerHelper.CreateTemplateDeploymentAsync(
                new ApplicationCredentials
                    {
                        AccessToken = managemenetToken,
                        SubscriptionId = endpoint.SubscriptionId,
                        TenantId = endpoint.TenantId,
                    },
                ResourceGroupOptions.ResourceGroup,
                ResourceGroupOptions.DeploymentName,
                Template, Parameters, appendTimestamp:ResourceGroupOptions.AppendTimeStamp, waitForDeployment: ResourceGroupOptions.WaitForDeploymentCompletion)
            .GetAwaiter().GetResult();

            Console.WriteLine($"Deployment Status: {result.Properties.ProvisioningState}");
            Output = result.Properties.Outputs as JObject;
            Console.WriteLine("\tOutput:");
            Console.WriteLine("\t\t" + Output?.ToString(Formatting.Indented).Replace("\n", "\n\t\t").TrimEnd('\t'));

            foreach (var prop in OutVariables)
            {
                
                var value = Output.SelectToken($"{prop.Key}.value");
                if (value != null)
                    TaskHelper.SetVariable(prop.Value.ToString(), JsonConvert.SerializeObject(value));
            }




        }
    }

}
