using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.Models
{
    public class SourceDefinition
    {
        public string Endpoint { get; set; }
        public string Target { get; set; }
        public string AuthKey { get; set; }
        public string Selector { get; set; }
        public string KeySelector { get; set; }
    }
}
