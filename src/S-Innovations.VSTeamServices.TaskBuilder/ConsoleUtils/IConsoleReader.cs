

namespace SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils
{
    using System.Reflection;
    using CommandLine;

    public interface IConsoleReader
    {
        void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info);
    }
}
