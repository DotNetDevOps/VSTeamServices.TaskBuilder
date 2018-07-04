/*
 * Copyright 2016 S-Innovations v/Poul K. Sørensen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace SInnovations.VSTeamServices.TaskBuilder.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
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
