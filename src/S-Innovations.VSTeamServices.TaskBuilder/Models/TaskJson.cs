using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SInnovations.VSTeamServices.TaskBuilder.Models
{
    public class TaskJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public string[] Visibility { get; set; }
        public string[] Demands { get; set; }

        public string Author { get; set; }
        public Version Version { get; set; }
        public string MinimumAgentVersion { get; set; }
        public Group[] Groups { get; set; }

        public TaskInput[] Inputs { get; set; }

        public string InstanceNameFormat { get; set; }

        public TaskExecution Execution { get; set; }

        public SourceDefinition[] SourceDefinitions { get; set; }
    }
}
