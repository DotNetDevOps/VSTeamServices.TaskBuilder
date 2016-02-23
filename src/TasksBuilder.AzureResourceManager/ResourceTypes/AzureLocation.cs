
namespace SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes
{
    using Newtonsoft.Json.Linq;
    using SInnovations.VSTeamServices.TasksBuilder.Models;
    using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

    public class AzureLocation : ITaskInputFactory
    {
        public Group[] CreateGroups()
        {
            return new Group[] { };
        }

        public TaskInput[] CreateInputs(string groupName, TaskInput defaultTask)
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
            return new[] { defaultTask };
        }
    }
}
