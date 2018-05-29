

namespace SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using Builder;
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

        public static T RunParseAndHandleArguments<T>(Parser parser, Func<T> factory, string[] args) where T : new()
        {
            return parser.ParseArguments<T>(factory, args).MapResult(options =>
            {
                return MapResult(parser, args, options);
            }, (o) => default(T));

        }
        public static T RunParseAndHandleArguments<T>(Parser parser, string[] args)
        {
            return parser.ParseArguments<T>(args).MapResult(options =>
            {
                return MapResult(parser, args, options);
            }, (o) => default(T));
             

        }

        private static T MapResult<T>(Parser parser, string[] args, T options)
        {
            var props = typeof(T).GetProperties().Where(p =>
                                Attribute.IsDefined(p, typeof(DisplayAttribute)))
                                .Select(p => new { PropertyInfo = p, DisplayAttribute = p.GetCustomAttribute<DisplayAttribute>() })
                                .Where(p => p.DisplayAttribute.ResourceType != null)
                                .Select(p => new
                                {
                                    p.DisplayAttribute,
                                    p.PropertyInfo,
                                    h = (p.DisplayAttribute.ResourceType == p.PropertyInfo.PropertyType ||
                                        (p.DisplayAttribute.ResourceType.IsGenericTypeDefinition && p.DisplayAttribute.ResourceType == p.PropertyInfo.PropertyType.GetGenericTypeDefinition()))
                                            ? p.PropertyInfo.GetValue(options)
                                                ?? Activator.CreateInstance(p.DisplayAttribute.ResourceType.IsGenericTypeDefinition
                                                    ? p.PropertyInfo.PropertyType
                                                    : p.DisplayAttribute.ResourceType)
                                            : Activator.CreateInstance(p.DisplayAttribute.ResourceType)
                                })
                                .OrderBy(p => p.DisplayAttribute.GetOrder() ?? 10).ToArray();

            foreach (var p in props)
            {
                var att = p.DisplayAttribute;
                var prop = p.PropertyInfo;
                var handler = p.h;
                // var att = prop.GetCustomAttribute<DisplayAttribute>();
                //if (att?.ResourceType != null)
                // {




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

                if (prop.PropertyType == att.ResourceType || att.ResourceType.IsSubclassOf(prop.PropertyType))
                {
                    prop.SetValue(options, handler);
                }
                else if (!prop.PropertyType.IsPrimitive)
                {
                    try
                    {
                        prop.SetValue(options, prop.GetValue(options) ?? Activator.CreateInstance(prop.PropertyType));
                    }
                    catch (MissingMethodException ctor)
                    {
                        Console.WriteLine("Warning: " + ctor.ToString());
                        Console.WriteLine(prop.PropertyType.FullName.ToString());
                    }
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

            return options;
        }

        public static T ParseAndHandleArguments<T>(string Message, string[] args, bool ignoreError = false) where T : new()
        {
            if(args.Length == 1 && args.First() == "--build")
            {
                TaskBuilder.BuildSelf();
                Environment.Exit(0);
            }


            if(args.Length >1 && args.First() == "--publish")
            {
       
                TaskBuilder.PublishSelf(args).Wait();
                Environment.Exit(0);
            }

            Console.WriteLine(string.Join(" ", args));
            args = MoveBoolsLast(args);
            Console.WriteLine(Message);
            if (Environment.GetEnvironmentVariable("SYSTEM_DEBUG") == "true")
            {
                Console.WriteLine(string.Join(" ", args));
            }

           // var options = new T();
            var b = new CommandLine.Parser((s) =>
            {
                s.IgnoreUnknownArguments = true;

            });

            var result = RunParseAndHandleArguments<T>(b, args);

            if (result != null || ignoreError)
                return result;

            throw new ArgumentException("Arguments not working " + string.Join(" ", args));
        }
    }
}
