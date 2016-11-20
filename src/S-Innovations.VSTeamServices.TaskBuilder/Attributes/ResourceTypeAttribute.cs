using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.Attributes
{
    public class ResourceTypeAttribute : Attribute
    {
        public string TaskInputType { get; set; }
    }
}
