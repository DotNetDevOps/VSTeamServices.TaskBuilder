using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.Models
{
    [JsonConverter(typeof(TaskExecutionConverter))]
    public class TaskExecution
    {
        public string ExecutionType { get; set; }
        public string Target { get; set; }
        public string ArgumentFormat { get; set; }
        public string WorkingDirectory { get; set; }
    }
}
