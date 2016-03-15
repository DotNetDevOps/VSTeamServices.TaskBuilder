﻿



namespace SInnovations.VSTeamServices.TasksBuilder.ResourceTypes
{
    using Attributes;
    using SInnovations.VSTeamServices.TasksBuilder.Models;
    using Tasks;
    public interface ITaskInputFactory
    {
        //  TaskInput[] CreateInputs(string groupName, TaskInput defaultTask);
        //  Group[] CreateGroups();

        TaskGeneratorResult GenerateTasks(string groupName, TaskInput defaultTask, SourceDefinitionAttribute parent);
    }
}
