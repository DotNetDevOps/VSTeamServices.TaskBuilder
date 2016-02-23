﻿

namespace SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using CommandLine;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

    public class ServiceEndpoint : IConsoleReader
    {

        public ServiceEndpoint()
        {
            _ctx = new Lazy<AuthenticationContext>(() =>
            {
                var ctx = new AuthenticationContext($"https://login.microsoftonline.com/{TenantId}");
                return ctx;
            });
        }
        private Lazy<AuthenticationContext> _ctx;
        private Dictionary<string, Lazy<AuthenticationResult>> _result = new Dictionary<string, Lazy<AuthenticationResult>>();

        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string PrincipalId { get; set; }
        public string PrincipalKey { get; set; }

        public string GetToken(string resourceUri)
        {
            if (!_result.ContainsKey(resourceUri))
            {
                _result.Add(resourceUri, new Lazy<AuthenticationResult>(() =>
                {
                    var cred = new ClientCredential(PrincipalId, PrincipalKey);
                    var token = _ctx.Value.AcquireToken(resourceUri, cred);
                    return token;
                }));
            }
            return _result[resourceUri].Value.AccessToken;
        }

        public void OnConsoleParsing(Parser parser, string[] args, object options, PropertyInfo info)
        {
            var serviceEndpoint = new ConnectedServiceEndpointOptions(this);
            parser.ParseArguments(args, serviceEndpoint);
        }
    }
}
