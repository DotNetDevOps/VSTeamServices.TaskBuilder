using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class AllowedValueOptionAttribute : Attribute
    {
        public string ParameterName { get; set; }
        public string OptionValue { get; set; }
        public string OptionName { get; set; }

        public AllowedValueOptionAttribute()
        {

        }

        public AllowedValueOptionAttribute(string parameterName, string optionValue, string optionName)
        {
            ParameterName = parameterName;
            OptionValue = optionValue;
            OptionName = optionName;
        }
    }
}
