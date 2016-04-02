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

        /// <summary>
        /// Get a task name from either  <see cref="DisplayAttribute"/> ShortName or <see cref="BaseOptionAttribute"/> 
        /// long name in this specific order with fallback to the clr property name;
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string GetVariableName(MemberInfo property)
        {
            var i = property.GetCustomAttribute<BaseOptionAttribute>();
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.ShortName ?? i?.LongName ?? property.Name;
        }
        public static JToken GetTaskDefaultValue(PropertyInfo property)
        {
            var i = property.GetCustomAttribute<BaseOptionAttribute>();
            object defaultValue = i?.DefaultValue ?? "";
            if (property.PropertyType == typeof(bool))
            {
                defaultValue = (defaultValue.GetType() == typeof(bool) && (bool)defaultValue) ? "true" : "false";
            }

            return JToken.FromObject(defaultValue);
        }
        public static Type GetResourcetype(PropertyInfo property)
        {
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.ResourceType;
        }
        public static string GetHelpmarkDown(PropertyInfo property)
        {
            var i = property.GetCustomAttribute<BaseOptionAttribute>();
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.Description ?? i?.HelpText;
        }
        public static string GetLabel(PropertyInfo property)
        {
            var i = property.GetCustomAttribute<BaseOptionAttribute>();
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.Name ?? i?.LongName;
        }
        public static string GetGroupName(PropertyInfo property)
        {
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.GroupName;
        }
        public static bool GetRequired(PropertyInfo property, PropertyInfo parent)
        {
            var i = property.GetCustomAttribute<BaseOptionAttribute>();
            var r = property.GetCustomAttribute<RequiredAttribute>();
            var ip = property.GetCustomAttribute<BaseOptionAttribute>();
            var rp = property.GetCustomAttribute<RequiredAttribute>();

            var p = (rp == null ? ip?.Required ?? false : true);

            return (r == null ? i?.Required ??p: true);
        }
        public static int GetOrder(PropertyInfo property)
        {
            var d = property.GetCustomAttribute<DisplayAttribute>();
            return d?.GetOrder() ?? 0;
        }
        public static TaskGeneratorResult GetTaskInputs(Type programOptionsType, PropertyInfo parent)
        {


            var instance = Activator.CreateInstance(programOptionsType);
            var properties = programOptionsType.GetProperties(
                BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).Where(p =>
                    Attribute.IsDefined(p, typeof(BaseOptionAttribute)) || Attribute.IsDefined(p, typeof(DisplayAttribute)))
                    .ToArray();

            //     var useServiceEndpoint = properties.Any(p => Attribute.IsDefined(p, typeof(ServiceEndpointAttribute)));


            var results = new TaskGeneratorResult();
            foreach (var property in properties)
            {


                var groupName = GetGroupName(property);
                var resourceType = GetResourcetype(property);
                var variableName = GetVariableName(property);


                var defaultTask = new TaskInput()
                {
                    HelpMarkDown = GetHelpmarkDown(property),
                    Name = variableName,
                    DefaultValue = GetTaskDefaultValue(property),
                    Label = GetLabel(property),
                    GroupName = groupName,
                    Required = GetRequired(property, parent),
                    VisibleRule = property.GetCustomAttribute<VisibleRuleAttribute>()?.VisibleRule,
                    Order = GetOrder(property),
                };
                defaultTask.Properties.EditableOptions = "True";

                if (variableName.Contains(" "))
                {
                    throw new ArgumentException($"The generated name for the given TaskInput contains spaces, please fix. : {defaultTask.Name}");
                }

                var sd = property.GetCustomAttribute<SourceDefinitionAttribute>();
                if (sd != null)
                {
                    try {
                        if (!sd.Ignore)
                        {
                            results.SourceDefinitions.Add(new SourceDefinition
                            {
                                Endpoint = sd.Endpoint,
                                AuthKey = (Activator.CreateInstance(sd.ConnectedService ?? parent.GetCustomAttribute<SourceDefinitionAttribute>()?.ConnectedService) as AuthKeyProvider).GetAuthKey(),
                                Selector = sd.Selector,
                                KeySelector = sd.KeySelector ?? "",
                                Target = variableName
                            });
                        }

                    } catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                if (typeof(ITaskInputFactory).IsAssignableFrom(resourceType))
                {
                    var fac = (property.GetValue(instance) as ITaskInputFactory) ?? (ITaskInputFactory)Activator.CreateInstance(resourceType);
                    var tasks = fac.GenerateTasks(groupName, defaultTask, property,instance);
                    foreach (var iput in tasks.Inputs)
                        iput.GroupName = iput.GroupName ?? defaultTask.GroupName;
                    results.Add(tasks);

                }
                else {
                    defaultTask.Type = GetTaskInputType(resourceType,property);
                    results.Inputs.Add(defaultTask);
                }


            }
            results.Groups.AddRange(
                programOptionsType.GetCustomAttributes<GroupAttribute>()
                      .Select(g => new Group
                      {
                          DisplayName = g.DisplayName,
                          Name = g.Name,
                          IsExpanded = g.isExpanded
                      }));

            return results;


        }



       // private static string GlobPathString = typeof(GlobPath).ToString();

        private static string GetTaskInputType(Type propertyType, PropertyInfo propertyInfo)
        {
            propertyType = propertyType ?? propertyInfo.PropertyType;

            if(propertyType == typeof(string) && propertyInfo.GetCustomAttribute< SourceDefinitionAttribute >() != null)
            {
                return "pickList";
            }

            switch (propertyType.ToString())
            {
                case "System.Nullable`1[System.Int]":
                case "System.Int":
                case "System.String":
                    return "string";
                case "System.Boolean":
                case "System.Nullable`1[System.Boolean]":
                    return "boolean";
                   

            };

            var type = propertyType.GetCustomAttribute<ResourceTypeAttribute>()?.TaskInputType;
            if (!string.IsNullOrEmpty(type))
            {
                return type;
            }

            throw new NotImplementedException(propertyType.ToString());
        }
    }
}
