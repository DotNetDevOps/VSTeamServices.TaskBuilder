using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.Extensions;
using SInnovations.VSTeamServices.TasksBuilder.Models;

namespace SInnovations.VSTeamServices.TasksBuilder.ResourceTypes
{
    public class Tags : IConsoleReader, ITaskInputFactory
    {
        public Dictionary<string, string> Values;
        public Group[] CreateGroups()
        {
            return new Group[] { };
        }

        public TaskInput[] CreateInputs(string groupName, TaskInput defaultTask)
        {
            defaultTask.Type = "string";

            return new[] { defaultTask };
        }

        public void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info)
        {
            if (info.GetValue(options) == this)
            {
                Values = new Dictionary<string, string>();
            }
            else
            {
                Values = info.GetValue(options) as Dictionary<string, string>;
            }

            var att = info.GetCustomAttribute<DisplayAttribute>();
            var idx = Array.IndexOf(args, $"--{att.ShortName}");
            if (idx != -1)
            {
                var tags = args[idx + 1];
                Values.MergeChanged(tags.Split(',').Select(k => k.Split(':')).ToDictionary(k => k[0], v => v[1]));
            }

        }
    }
}
