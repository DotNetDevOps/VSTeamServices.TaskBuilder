

using Newtonsoft.Json;

namespace SInnovations.VSTeamServices.TasksBuilder.Models
{
    public class TaskInputProperties
    {
        [JsonProperty(PropertyName = "EditableOptions")]
        public string EditableOptions { get; set; }
    }
}
