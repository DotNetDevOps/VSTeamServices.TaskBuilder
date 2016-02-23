

namespace SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using CommandLine;


    public class ConsoleHelper
    {
        public static string[] MoveBoolsLast(string[] args)
        {
            if(args.Length == 0)
            {
                return args;
            }

            var bools = new List<string>();
            var rest = new List<string>();
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i - 1].StartsWith("-") && args[i].StartsWith("-"))
                    bools.Add(args[i - 1]);
                else
                    rest.Add(args[i - 1]);
            }
            rest.Add(args[args.Length - 1]);

            return rest.Concat(bools).ToArray();
        }
        public static bool ParseAndHandleArguments<T>(Parser parser, string[] args, T options)
        {
            var ignoreError = parser.ParseArguments(args, options);

            if (ignoreError)
            {

                var props = typeof(T).GetProperties().Where(p =>
                    Attribute.IsDefined(p, typeof(DisplayAttribute)))
                    .Select(p => new { p, a = p.GetCustomAttribute<DisplayAttribute>() })
                    .Where(p => p.a.ResourceType != null)
                    .Select(p => new { p.a, p.p, h = (p.a.ResourceType == p.p.PropertyType) ? p.p.GetValue(options) ?? Activator.CreateInstance(p.a.ResourceType) : Activator.CreateInstance(p.a.ResourceType) })
                    .ToArray();

                foreach (var p in props)
                {
                    var att = p.a;
                    var prop = p.p;
                    var handler = p.h;
                    // var att = prop.GetCustomAttribute<DisplayAttribute>();
                    //if (att?.ResourceType != null)
                    // {


                    if (prop.PropertyType == att.ResourceType)
                    {
                        prop.SetValue(options, handler);
                    }
                    else
                    {
                        prop.SetValue(options, prop.GetValue(options) ?? Activator.CreateInstance(prop.PropertyType));
                    }

                    if (handler is IConsoleReader)
                    {
                        var consoleReader = handler as IConsoleReader;
                        consoleReader.OnConsoleParsing(parser, args, options, prop);
                    }

                    if (handler is IConsoleReader<T>)
                    {
                        var consoleReader = handler as IConsoleReader<T>;
                        consoleReader.OnConsoleParsing(parser, args, options, prop);
                    }

                    //  }
                }

                foreach (var p in props)
                {
                    if (p.h is IConsoleExecutor)
                    {
                        (p.h as IConsoleExecutor).Execute(options);
                    }
                    if (p.h is IConsoleExecutor<T>)
                        (p.h as IConsoleExecutor<T>).Execute(options);
                }


            }

            return ignoreError;

        }
        public static T ParseAndHandleArguments<T>(string Message, string[] args, bool ignoreError = false) where T : new()
        {
            Console.WriteLine(string.Join(" ", args));
            args = MoveBoolsLast(args);
            Console.WriteLine(Message);
            if (Environment.GetEnvironmentVariable("SYSTEM_DEBUG") == "true")
            {
                Console.WriteLine(string.Join(" ", args));
            }

            var options = new T();
            var b = new CommandLine.Parser((s) =>
            {
                s.IgnoreUnknownArguments = true;

            });

            ignoreError = ParseAndHandleArguments(b, args, options);

            if (ignoreError)
                return options;

            throw new ArgumentException("Arguments not working " + string.Join(" ", args));
        }
    }
}
