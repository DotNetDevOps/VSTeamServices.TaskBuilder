


namespace SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils
{
    using System.Reflection;
    using CommandLine;

    public interface IConsoleReader<T>
    {
        void OnConsoleParsing(Parser parser, string[] args, T options, PropertyInfo info);
    }
}
