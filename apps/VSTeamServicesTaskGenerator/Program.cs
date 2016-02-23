using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SInnovations.VSTeamServices.TasksBuilder.Builder;

namespace VSTeamServicesTaskGenerator
{
    class Program
    {
        static void Main(string[] arguments)
        {
            foreach (var arg in arguments)
            {
                TaskBuilder.BuildTask(arg);
            }
        }
    }
}
