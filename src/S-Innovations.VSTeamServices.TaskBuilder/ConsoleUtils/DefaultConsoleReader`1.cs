using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.Models;
using SInnovations.VSTeamServices.TaskBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils
{
    /// <summary>
    /// Base Implementation of a Console Reader and TaskFactory, where input properties will be generated for T and also parsed when executing.
    /// </summary>
    /// <typeparam name="T">The Console Parsing Object Type</typeparam>
    public abstract class DefaultConsoleReader<T> : IConsoleReader, ITaskInputFactory
    {
        public DefaultConsoleReader()
        {
         
        }
        protected T Options { get; set; }

        public virtual void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info)
        {
            if (Options == null)
            {
                Options = ConsoleHelper.RunParseAndHandleArguments<T>(parser, args);
            }else
            {
                ConsoleHelper.RunParseAndHandleArguments(parser,()=>(object)Options, args);
            }        
        }

      


        public TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, PropertyInfo parent)
        {
            var tasks = TaskHelper.GetTaskInputs(typeof(T), parent);
           
            return tasks;
        }
    }
}
