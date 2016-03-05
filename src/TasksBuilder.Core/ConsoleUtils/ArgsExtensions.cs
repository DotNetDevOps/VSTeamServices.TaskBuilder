using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils
{
    public static class ArgsExtensions
    {
        public static string[] LoadFrom<T>(this string[] args, string path, params Expression<Func<T, object>>[] extra)
        {

            //var ob = new T();
            var props = extra.Select(propertyLambda =>
            {
                BinaryExpression bi = propertyLambda.Body as BinaryExpression;
                if (bi == null)
                {
                    var un = propertyLambda.Body as UnaryExpression;
                    bi = un?.Operand as BinaryExpression;
                    if (bi == null)
                    {
                        throw new ArgumentException(string.Format(
                            "Expression '{0}' refers to a method, not a property.",
                            propertyLambda.ToString()));
                    }
                }

                MemberExpression member = bi.Left as MemberExpression;
                PropertyInfo propInfo = member.Member as PropertyInfo;
                if (propInfo == null)
                    throw new ArgumentException(string.Format(
                        "Expression '{0}' refers to a field, not a property.",
                        propertyLambda.ToString()));



                var value = (bi.Right as ConstantExpression)?.Value ??
                    ((bi.Right as MemberExpression)?.Expression as ConstantExpression)?.Value;



                var op = propInfo.GetCustomAttribute<OptionAttribute>();

                return new[] { $"-{op?.ShortName?.ToString() ?? $"-{op.LongName}" }", value.ToString() };
            });
            if(string.IsNullOrEmpty(path))
                return args.Concat(props.SelectMany(a => a)).ToArray();

            return args.Concat(File.ReadAllLines(path).Concat(props.SelectMany(a => a))).ToArray();

            return args;
        }
    }
}
