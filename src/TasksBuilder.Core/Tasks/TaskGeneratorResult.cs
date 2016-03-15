
namespace SInnovations.VSTeamServices.TasksBuilder.Tasks
{
    using System.Collections.Generic;
    using SInnovations.VSTeamServices.TasksBuilder.Models;

    public class TaskGeneratorResult
    {
        public TaskGeneratorResult()
        {
            Groups = new List<Group>();
            Inputs = new List<TaskInput>();
            SourceDefinitions = new List<SourceDefinition>();
        }
        public List<Group> Groups { get; set; }

        public List<TaskInput> Inputs { get; set; }
        public List<SourceDefinition> SourceDefinitions { get; set; }

        public void Add(TaskGeneratorResult other)
        {
            this.Groups.AddRange(other.Groups);
            this.Inputs.AddRange(other.Inputs);
            this.SourceDefinitions.AddRange(other.SourceDefinitions);
        }
    }
}
