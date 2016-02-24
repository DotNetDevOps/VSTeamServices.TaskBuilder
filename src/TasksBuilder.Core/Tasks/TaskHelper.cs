using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace SInnovations.VSTeamServices.TasksBuilder.Tasks
{
    public class TaskHelper
    {
        public static void SetVariable(string variableName, string value)
        {
            Console.WriteLine($"##vso[task.setvariable variable={variableName};]{value}");
        }

        public static TaskGeneratorResult GetTaskInputs(Type programOptionsType)
        {

            var result = new TaskGeneratorResult();

            var properties = programOptionsType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Where(p =>
                    Attribute.IsDefined(p, typeof(BaseOptionAttribute)) || Attribute.IsDefined(p, typeof(DisplayAttribute)))
                    .ToArray();

            //     var useServiceEndpoint = properties.Any(p => Attribute.IsDefined(p, typeof(ServiceEndpointAttribute)));


            var groups = new List<Group>();

            result.Inputs = properties.Select(property =>
            {
                var i = property.GetCustomAttribute<BaseOptionAttribute>();
                var d = property.GetCustomAttribute<DisplayAttribute>();
                var r = property.GetCustomAttribute<RequiredAttribute>();
                object defaultValue = i?.DefaultValue ?? "";

                if (property.PropertyType == typeof(bool))
                {
                    defaultValue = (defaultValue.GetType() == typeof(bool) && (bool)defaultValue) ? "true" : "false";
                }

                var resourceType = d?.ResourceType ?? typeof(string);
                var defaultTask = new TaskInput()
                {
                    HelpMarkDown = d?.Description ?? i?.HelpText,
                    Name = d?.ShortName ?? i?.LongName, //VariableName
                    DefaultValue = JToken.FromObject(defaultValue),
                    Label = d?.Name ?? i?.LongName,
                    GroupName = d?.GroupName,
                    Required = i?.Required ?? (r == null ? false : true),
                    VisibleRule = property.GetCustomAttribute<VisibleRuleAttribute>()?.VisibleRule,
                    Order = d?.Order ?? 0
                };
                if (defaultTask.Name?.Contains(" ") ?? false)
                {
                    throw new ArgumentException($"The generated name for the given TaskInput contains spaces, please fix. : {defaultTask.Name}");
                }

                if (typeof(ITaskInputFactory).IsAssignableFrom(resourceType))
                {
                    var fac = (Activator.CreateInstance(resourceType) as ITaskInputFactory);
                    groups.AddRange(fac.CreateGroups());
                    return fac.CreateInputs(d?.GroupName, defaultTask);
                }
                else {
                    defaultTask.Type = NewMethod(d?.ResourceType ?? property.PropertyType);
                    return new[] { defaultTask };
                }

            }).SelectMany(k => k).ToArray();

            result.Groups = programOptionsType.GetCustomAttributes<GroupAttribute>()
              .Select(g => new Group
              {
                  DisplayName = g.DisplayName,
                  Name = g.Name,
                  IsExpanded = g.isExpanded
              }).Concat(groups).ToArray();

            return result;
        }
        private static string NewMethod(Type property)
        {
            switch (property.ToString())
            {
                case "System.String":
                    return "string";
                case "SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes.ServiceEndpoint":
                    return "connectedService:AzureRM";
                case "System.Boolean":
                case "System.Nullable`1[System.Boolean]":
                    return "boolean";

            };

            throw new NotImplementedException(property.ToString());
        }
    }
}
