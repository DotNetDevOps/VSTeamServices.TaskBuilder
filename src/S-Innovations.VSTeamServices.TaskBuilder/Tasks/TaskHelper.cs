using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.Models;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;

namespace SInnovations.VSTeamServices.TaskBuilder.Tasks
{
    public class TaskHelper
    {
        public static void SetVariable(string variableName, string value)
        {
            Console.WriteLine($"##vso[task.setvariable variable={variableName};]{value}");
        }

        public static void SetVariable(string variableName, string value, bool isSecret)
        {
            if (isSecret)
            {
                Console.WriteLine($"##vso[task.setvariable variable={variableName};issecret=true;]{value}");
            }else
            {
                SetVariable(variableName, value);
            }
        }
        /// <summary>
        /// Get a task name from either  <see cref="DisplayAttribute"/> ShortName or <see cref="BaseOptionAttribute"/> 
        /// long name in this specific order with fallback to the clr property name;
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static string GetVariableName(MemberInfo property)
        {
            var i = property.GetCustomAttribute<OptionAttribute>();
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.ShortName ?? i?.LongName ?? property.Name;
        }
        public static JToken GetTaskDefaultValue(PropertyInfo property)
        {
          
            object defaultValue = property.GetCustomAttribute<BaseAttribute>()?.Default ??
                (property.GetCustomAttribute<DefaultValueAttribute>()?.Value) ?? "";
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
            var i = property.GetCustomAttribute<OptionAttribute>();
            var d = property.GetCustomAttribute<DisplayAttribute>();

            return d?.Description ?? i?.HelpText;
        }
        public static string GetLabel(PropertyInfo property)
        {
            var i = property.GetCustomAttribute<OptionAttribute>();
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
            var i = property.GetCustomAttribute<OptionAttribute>();
            var r = property.GetCustomAttribute<RequiredAttribute>();
            var ip = property.GetCustomAttribute<OptionAttribute>();
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

       
            
            var properties = programOptionsType.GetProperties(
                BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).Where(p =>
                    Attribute.IsDefined(p, typeof(OptionAttribute)) || Attribute.IsDefined(p, typeof(DisplayAttribute)))
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
                                AuthKey = (Activator.CreateInstance(sd.ConnectedService ?? 
                                    parent?.GetCustomAttribute<SourceDefinitionAttribute>()?.ConnectedService ?? 
                                    programOptionsType.GetCustomAttribute<SourceDefinitionAttribute>()?.ConnectedService) as AuthKeyProvider).GetAuthKey(),
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
                    ITaskInputFactory fac= null;
                    if (resourceType == property.PropertyType)
                    {
                        fac = property.GetValue(Activator.CreateInstance(programOptionsType)) as ITaskInputFactory;
                    }
                    if(fac==null && resourceType.IsGenericTypeDefinition && resourceType == property.PropertyType.GetGenericTypeDefinition())
                    {
                        fac = property.GetValue(Activator.CreateInstance(programOptionsType)) as ITaskInputFactory;
                    }
                    if(fac==null)
                    {
                        fac = Activator.CreateInstance(resourceType) as ITaskInputFactory;
                    }
                    var tasks = fac.GenerateTasks(groupName, defaultTask, property);
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
                case "System.String":
                    if (Attribute.IsDefined(propertyInfo, typeof(MultilineAttribute)))
                        return "multiLine";
                    return "string";
                case "System.Boolean":
                case "System.Nullable`1[System.Boolean]":
                    return "boolean";

                case "System.Nullable`1[System.Int]":
                case "System.Int":
                case "System.Int32":
                    return "string";
            };

            var type = propertyType.GetCustomAttribute<ResourceTypeAttribute>()?.TaskInputType;
            if (!string.IsNullOrEmpty(type))
            {
                return type;
            }

            throw new NotImplementedException(propertyType.ToString());
        }
    }

    public class MultilineAttribute : Attribute
    {

    }
}
