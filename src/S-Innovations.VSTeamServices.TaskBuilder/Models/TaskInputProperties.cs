

using Newtonsoft.Json;

namespace SInnovations.VSTeamServices.TaskBuilder.Models
{
    public class TaskInputProperties
    {
        [JsonProperty(PropertyName = "EditableOptions")]
        public string EditableOptions { get; set; }
    }
}
