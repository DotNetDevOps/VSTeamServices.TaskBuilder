



namespace SInnovations.VSTeamServices.TasksBuilder.ResourceTypes
{
    using SInnovations.VSTeamServices.TasksBuilder.Models;

    public interface ITaskInputFactory
    {
        TaskInput[] CreateInputs(string groupName, TaskInput defaultTask);
        Group[] CreateGroups();
    }
}
