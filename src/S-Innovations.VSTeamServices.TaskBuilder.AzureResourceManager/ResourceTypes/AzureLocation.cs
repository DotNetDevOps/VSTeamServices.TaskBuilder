
namespace SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes
{
    using System;
    using System.Reflection;
    using Attributes;
    using Newtonsoft.Json.Linq;
    using SInnovations.VSTeamServices.TaskBuilder.Models;
    using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
    using Tasks;

    public class AzureLocation : ITaskInputFactory
    {
       

        public TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, PropertyInfo parent)
        {
            defaultTask.Type = "pickList";
            defaultTask.Properties.EditableOptions = "True";
            defaultTask.Options = new JObject(
                new JProperty("Australia East", "Australia East"),
                new JProperty("Australia Southeast", "Australia Southeast"),
                new JProperty("Brazil South", "Brazil South"),
                new JProperty("Central US", "Central US"),
                new JProperty("East Asia", "East Asia"),
                new JProperty("East US", "East US"),
                new JProperty("East US 2 ", "East US 2 "),
                new JProperty("Japan East", "Japan East"),
                new JProperty("Japan West", "Japan West"),
                new JProperty("North Central US", "North Central US"),
                new JProperty("North Europe", "North Europe"),
                new JProperty("South Central US", "South Central US"),
                new JProperty("Southeast Asia", "Southeast Asia"),
                new JProperty("West Europe", "West Europe"),
                new JProperty("West US", "West US")
            );

            var result = new TaskGeneratorResult();
            result.Inputs.Add(defaultTask);
            return result;
        }
    }
}
