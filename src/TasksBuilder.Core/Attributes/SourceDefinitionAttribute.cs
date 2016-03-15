using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace SInnovations.VSTeamServices.TasksBuilder.Attributes
{
    public class SourceDefinitionAttribute : Attribute
    {
        public SourceDefinitionAttribute(Type connectedService, string endpoint, string selector)
        {
            ConnectedService = connectedService;
            Endpoint = endpoint;
            Selector = selector;
        }

        public Type ConnectedService { get; private set; }

        public string Endpoint { get;private set; }
        public string KeySelector { get; internal set; }
        public string Selector { get; private set; }
    }

    public class ResourceGrounPickerAttribute : SourceDefinitionAttribute
    {
        public ResourceGrounPickerAttribute(Type connectedService)
            :base(  
                 connectedService, 
                 "https://management.azure.com/subscriptions/$(authKey.SubscriptionId)/resourcegroups?api-version=2015-01-01",
                 "jsonpath:$.value[*].name"
                 )
        {

        }
    }
    public interface AuthKeyProvider
    {
        string GetAuthKey();
    }
    public class PropertyRelation<TOwner, TProperty> : AuthKeyProvider
    {
        private readonly Expression<Func<TOwner, TProperty>> _propGetter;

        public PropertyRelation(Expression<Func<TOwner, TProperty>> propGetter)
        {
            _propGetter = propGetter;
        }

        public string GetAuthKey()
        {
           var member= _propGetter.Body as MemberExpression;
            return $"$({TaskHelper.GetVariableName(member.Member)})";

        }

        //public PropertyInfo GetPropertyInfo(TOwner owner)
        //{
        //    return _propGetter(owner);
        //}
    }
}
