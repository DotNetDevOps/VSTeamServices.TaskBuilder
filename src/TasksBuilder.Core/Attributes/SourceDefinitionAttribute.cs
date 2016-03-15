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
       
        public SourceDefinitionAttribute(Type connectedService, string endpoint, string selector, string keySelector=null) 
        {
            ConnectedService = connectedService;
            Endpoint = endpoint;
            Selector = selector;
            KeySelector = KeySelector;
        }

        public Type ConnectedService { get; private set; }

        public string Endpoint { get;private set; }
        public string KeySelector { get; internal set; }
        public string Selector { get; private set; }
    }
    public class ConnectedServiceRelationAttribute : SourceDefinitionAttribute
    {
        public ConnectedServiceRelationAttribute(Type connectedService) : base(connectedService,null,null)
        {
           
        }
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

    public class ArmResourceIdPickerAttribute : SourceDefinitionAttribute
    {
        public ArmResourceIdPickerAttribute(string provider, string apiVersion)
            : base(
                 null,
                 $"https://management.azure.com/subscriptions/$(authKey.SubscriptionId)/providers/{provider}?api-version={apiVersion}",
                 "jsonpath:$.value[*].name",
                 "jsonpath:$.value[*].id"
                 )
        {

        }
    }

    public class ArmResourceProviderPickerAttribute : SourceDefinitionAttribute
    {
        public ArmResourceProviderPickerAttribute(string id, string subTypes, string apiVersion)
            : base(
                 null,
                 $"https://management.azure.com/{id}/{subTypes??""}?api-version={apiVersion}",
                 "jsonpath:$.value[*].name",
                 "jsonpath:$.value[*].id"
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
