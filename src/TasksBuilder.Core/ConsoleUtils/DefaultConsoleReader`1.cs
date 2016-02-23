using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Models;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils
{
    public abstract class DefaultConsoleReader<T> : IConsoleReader, ITaskInputFactory where T : new()
    {
        public DefaultConsoleReader()
        {
            Options = new T();
        }
        public abstract T Options { get; set; }
        public virtual void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info)
        {

            if (ConsoleHelper.ParseAndHandleArguments<T>(parser, args, Options))
            {

            }
        }

        public Group[] CreateGroups()
        {
            return TaskHelper.GetTaskInputs(typeof(T)).Groups;
        }

        public TaskInput[] CreateInputs(string groupName, TaskInput defaultTask)
        {
            var result = TaskHelper.GetTaskInputs(typeof(T));
            return result.Inputs;
        }
    }
}
