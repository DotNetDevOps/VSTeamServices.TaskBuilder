using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes
{
    public class ConnectedServiceEndpointOptions
    {
        public ConnectedServiceEndpointOptions()
        {

        }
        public ConnectedServiceEndpointOptions(ServiceEndpoint endpoint)
        {
            ConnectedServiceName = endpoint;
        }
        private ServiceEndpoint ConnectedServiceName { get; set; }

        [Option("TenantId", HelpText = "TenantId")]
        public string TenantId
        {
            get { return ConnectedServiceName.TenantId; }
            set { ConnectedServiceName.TenantId = value; }
        }

        [Option("SubscriptionId", HelpText = "SubscriptionId")]
        public string SubscriptionId
        {
            get { return ConnectedServiceName.SubscriptionId; }
            set { ConnectedServiceName.SubscriptionId = value; }
        }

        [Option("PrincipalKey", HelpText = "PrincipalKey")]
        public string PrincipalKey
        {
            get { return ConnectedServiceName.PrincipalKey; }
            set { ConnectedServiceName.PrincipalKey = value; }
        }
        [Option("PrincipalId", HelpText = "PrincipalId")]
        public string PrincipalId
        {
            get { return ConnectedServiceName.PrincipalId; }
            set { ConnectedServiceName.PrincipalId = value; }
        }

    }
}
