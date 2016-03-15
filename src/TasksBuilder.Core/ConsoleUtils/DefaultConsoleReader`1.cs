using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils
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
            if (ConsoleHelper.ParseAndHandleArguments<T>(parser, args, Options))
            {

            }
        }

      


        public TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, SourceDefinitionAttribute parent)
        {
            return TaskHelper.GetTaskInputs(typeof(T));
        }
    }
}
