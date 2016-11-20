using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SInnovations.VSTeamServices.TaskBuilder.Tasks;

namespace SInnovations.VSTeamServices.TaskBuilder.Attributes
{

    public class SourceDefinitionAttribute : Attribute
    {
        bool _ignore;
        public SourceDefinitionAttribute(Type connectedService, string endpoint, string selector, string keySelector = null, bool ignore = false)
        {
            ConnectedService = connectedService;
            Endpoint = endpoint;
            Selector = selector;
            KeySelector = keySelector;
            _ignore = ignore;
        }

        public Type ConnectedService { get; private set; }

        public string Endpoint { get; private set; }
        public string KeySelector { get; internal set; }
        public string Selector { get; private set; }

        public bool Ignore {
            get { return _ignore || string.IsNullOrWhiteSpace(Endpoint); }
        }
    }

    public class ConnectedServiceRelationAttribute : SourceDefinitionAttribute
    {
        public ConnectedServiceRelationAttribute(Type connectedService) : base(connectedService, null, null)
        {

        }
    }
    public class ResourceGrounPickerAttribute : SourceDefinitionAttribute
    {
        public ResourceGrounPickerAttribute(Type connectedService)
            : base(
                 connectedService,
                 "https://management.azure.com/subscriptions/$(authKey.SubscriptionId)/resourcegroups?api-version=2015-01-01",
                 "jsonpath:$.value[*].name"
                 )
        {

        }
    }


    public class LocationPickerAttribute : SourceDefinitionAttribute
    {



        public LocationPickerAttribute(Type connectedServiceRelation)
           : base(
                 connectedServiceRelation,
                 "https://management.azure.com/subscriptions/$(authKey.SubscriptionId)/locations?api-version=2016-02-01",
                 "jsonpath:$.value[*].displayName",
                 "jsonpath:$.value[*].name"
                 )
        {

        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class ParameterSourceDefinitionAttribute : SourceDefinitionAttribute {

        public string ParameterName { get; private set; }
        public ParameterSourceDefinitionAttribute(Type connectedServiceRelation, string parameterName, string endpoint, string selector, string keySelector = null)
            : base(connectedServiceRelation, endpoint, selector, keySelector, true)
        {
            ParameterName = parameterName;
        }
    }

    public class ParameterLocationPickerAttribute : ParameterSourceDefinitionAttribute
    {

        public ParameterLocationPickerAttribute(Type connectedServiceRelation, string parameterName)
           : base(
                 connectedServiceRelation,
                 parameterName,
                 "https://management.azure.com/subscriptions/$(authKey.SubscriptionId)/locations?api-version=2016-02-01",
                 "jsonpath:$.value[*].displayName",
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
                 $"https://management.azure.com/{id}/{subTypes ?? ""}?api-version={apiVersion}",
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
    public  abstract class PropertyRelation{
        public abstract object GetProperty(object owner);
    }
    public class PropertyRelation<TOwner, TProperty> : PropertyRelation, AuthKeyProvider
    {
        private readonly Expression<Func<TOwner, TProperty>> _propGetter;

        public PropertyRelation(Expression<Func<TOwner, TProperty>> propGetter)
        {
            _propGetter = propGetter;
        }

        public string GetAuthKey()
        {
           var member= _propGetter.Body as MemberExpression;
            var variable= $"$({TaskHelper.GetVariableName(member.Member)})";
            return variable;
        }
        public override object GetProperty(object owner)
        {
            return _propGetter.Compile().DynamicInvoke(new object[] { owner });
        }
        public TProperty GetProperty(TOwner owner)
        {
            return (TProperty)GetProperty((object)owner);
        }
        //public PropertyInfo GetPropertyInfo(TOwner owner)
        //{
        //    return _propGetter(owner);
        //}
    }
}
