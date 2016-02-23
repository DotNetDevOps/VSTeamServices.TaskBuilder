﻿

namespace SInnovations.VSTeamServices.TasksBuilder.Attributes
{
    using System;
    public class VisibleRuleAttribute : Attribute
    {
        public string VisibleRule { get; set; }
        public VisibleRuleAttribute(string rule)
        {
            VisibleRule = rule;
        }
    }
}
