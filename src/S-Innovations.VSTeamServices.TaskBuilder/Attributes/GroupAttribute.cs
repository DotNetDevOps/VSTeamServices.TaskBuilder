

namespace SInnovations.VSTeamServices.TaskBuilder.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GroupAttribute : Attribute
    {
        public string Name { get; set; }
        public bool isExpanded { get; set; }
        public string DisplayName { get; set; }

    }
}
