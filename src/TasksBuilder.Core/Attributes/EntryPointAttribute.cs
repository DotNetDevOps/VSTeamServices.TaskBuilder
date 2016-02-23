



namespace SInnovations.VSTeamServices.TasksBuilder.Attributes
{

    using System;

    public class EntryPointAttribute : Attribute
    {
        public string InstanceFormat;

        public EntryPointAttribute(string v)
        {
            this.InstanceFormat = v;
        }
    }
}
