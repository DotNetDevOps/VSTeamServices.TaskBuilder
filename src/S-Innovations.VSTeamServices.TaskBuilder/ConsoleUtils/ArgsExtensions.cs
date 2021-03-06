﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils
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



                var value = (bi.Right as ConstantExpression)?.Value ?? GetValue(bi.Right as MemberExpression);



                var op = propInfo.GetCustomAttribute<OptionAttribute>();
                if ((op?.ShortName?.Length ?? 0) > 0)
                {
                    return new[] { $"-{op.ShortName}", value.ToString() };
                }

                return new[] { $"--{op.LongName}", value.ToString() };
            });
            if(string.IsNullOrEmpty(path))
                return args.Concat(props.SelectMany(a => a)).ToArray();

            return args.Concat(File.ReadAllLines(path).Concat(props.SelectMany(a => a))).ToArray();

            return args;
        }
        private static object GetValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));

            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            var getter = getterLambda.Compile();

            return getter();
        }
    }
}
