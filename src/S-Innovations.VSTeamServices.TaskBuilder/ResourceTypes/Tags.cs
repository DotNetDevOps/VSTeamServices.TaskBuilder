﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TaskBuilder.Extensions;
using SInnovations.VSTeamServices.TaskBuilder.Models;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.ResourceTypes
{
    public class Tags : IConsoleReader, ITaskInputFactory
    {
        public Dictionary<string, string> Values;
        public Group[] CreateGroups()
        {
            return new Group[] { };
        }

       

        public TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, PropertyInfo parent)
        {
            defaultTask.Type = "string";


            return new TaskGeneratorResult { Inputs = new List<TaskInput>{ defaultTask } };
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
