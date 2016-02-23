﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.ResourceManager;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.Extensions;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager
{
    public abstract class ArmTemplateDeployment<T> : IConsoleExecutor<T>, ITaskInputFactory, IConsoleReader<T>
    {
        public Func<T, ServiceEndpoint> EndpointProvider { get; private set; }

        public ArmTemplateDeployment(Func<T, ServiceEndpoint> endPointProvider)
        {
            EndpointProvider = endPointProvider;
            ResourceGroupOptions = new ResourceGroupOptions();
        }
        public JObject Output { get; set; }
        public JObject Parameters { get; set; }
        public ResourceGroupOptions ResourceGroupOptions { get; private set; }


        public abstract JObject LoadTemplateParameters();
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

            var parametersObj = ParamterTypeGenerator.CreateNewObject(LoadTemplateParameters());
            if (!parser.ParseArguments(args, parametersObj))
            {
                Console.WriteLine($"Failed to read parameterObj: {JObject.FromObject(parametersObj).ToString(Formatting.Indented)} ");
                return;
            }

            Parameters = new JObject(
                    JObject.FromObject(parametersObj).Properties()
                        .Where(p => p.Value.ToObject<string>() != null)
                        .Select(p => ResourceManagerHelper.CreateValue(p.Name, p.Value)
                ));

        }

        public string GetOutputValue(string name)
        {
            return Output.SelectToken($"{name}.value").ToString();

        }
        public virtual Group[] CreateGroups()
        {
            return TaskHelper.GetTaskInputs(typeof(ResourceGroupOptions)).Groups;
        }

        public virtual TaskInput[] CreateInputs(string groupName, TaskInput defaultTask)
        {


            var result = TaskHelper.GetTaskInputs(typeof(ResourceGroupOptions));

            return LoadTemplateParameters().OfType<JProperty>().Select(t =>
            {
                var obj = t.Value as JObject;
                var allowedValues = obj.SelectToken("allowedValues")?.ToObject<string[]>() ?? new string[] { };
                JObject options = null;
                if (allowedValues.Any())
                {
                    options = new JObject(allowedValues.Select(k => new JProperty(k, k.Humanize(LetterCasing.Title))));
                }


                return new TaskInput
                {
                    Name = t.Name,
                    DefaultValue = obj.SelectToken("defaultValue")?.Value<string>(),
                    GroupName = groupName,
                    HelpMarkDown = obj.SelectToken("metadata.description")?.Value<string>(),
                    Type = allowedValues.Any() ? "pickList" : GetTaskType(obj),
                    Label = t.Name.Humanize(LetterCasing.Title),
                    Required = string.IsNullOrEmpty(obj.SelectToken("defaultValue")?.Value<string>()),
                    Options = options
                };
            }).Concat(result.Inputs).ToArray();



        }

        private static string GetTaskType(JObject obj)
        {
            var type = obj.SelectToken("type")?.Value<string>();
            switch (type)
            {
                case "bool":
                    return "boolean";
                case "string":
                case "pickList":
                    return type;
            }

            throw new ArgumentException($"{type} not known");

        }

        public virtual void Execute(T options)
        {

            var template = LoadTemplate(options);

            Console.WriteLine("Deploying Resource Group: ");
            Console.WriteLine("\tParameters:");
            Console.WriteLine(Parameters.ToString(Formatting.Indented));
            var endpoint = EndpointProvider(options);
            var managemenetToken = endpoint.GetToken("https://management.azure.com/");

            if (ResourceGroupOptions.CreateResourceGroup)
            {
                Console.WriteLine("Creating or Updating Resource Group");
                var rg = ResourceManagerHelper.CreateResourceGroupIfNotExistAsync(
                    endpoint.SubscriptionId, managemenetToken,
                    ResourceGroupOptions.ResourceGroup,
                    ResourceGroupOptions.ResourceGroupLocation).GetAwaiter().GetResult();

                if (rg.Tags.MergeChangedReversed(ResourceGroupOptions.Tags))
                {
                    rg = ResourceManagerHelper.UpdateResourceGroupAsync(endpoint.SubscriptionId, managemenetToken, rg)
                        .GetAwaiter().GetResult();
                }

                Console.WriteLine($"{rg.Name} : {string.Join(" ", rg.Tags.Select(k => $"[{k.Key}:{k.Value}]"))}");


            }

            var result = ResourceManagerHelper.CreateTemplateDeploymentAsync(new ApplicationCredentials
            {
                AccessToken = managemenetToken,
                SubscriptionId = endpoint.SubscriptionId,
                TenantId = endpoint.TenantId,
            },
            ResourceGroupOptions.ResourceGroup,
            ResourceGroupOptions.DeploymentName,
            template, Parameters).GetAwaiter().GetResult();
            Console.WriteLine($"Deployment Status: {result.Properties.ProvisioningState}");
            Output = result.Properties.Outputs as JObject;





        }
    }

}