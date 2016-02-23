﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SInnovations.VSTeamServices.TasksBuilder.Models
{
    public class TaskInput
    {
        public TaskInput()
        {
            Properties = new TaskInputProperties();
        }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public JToken DefaultValue { get; set; }
        public bool Required { get; set; }
        public string GroupName { get; set; }
        public string VisibleRule { get; set; }
        public string HelpMarkDown { get; set; }
        public JObject Options { get; set; }

        public TaskInputProperties Properties { get; set; }
    }
}