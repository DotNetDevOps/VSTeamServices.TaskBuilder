using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.KeyVault;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

namespace SInnovations.VSTeamServices.TasksBuilder.KeyVault.ResourceTypes
{
    public class KeyVaultOutput<T> : DefaultConsoleReader<KeyVaultOptions>, IConsoleReader<T>
    {
        public Func<T, ServiceEndpoint> EndpointProvider { get; private set; }

        /// <summary>
        /// Parameter less constructor for generating Options.
        /// </summary>
        public KeyVaultOutput()
        {
       
        }

        /// <summary>
        /// Runtime constructor
        /// </summary>
        /// <param name="endPointProvider"></param>
        public KeyVaultOutput(Func<T, ServiceEndpoint> endPointProvider)
        {
            EndpointProvider = endPointProvider;
        }
       
        /// <summary>
        /// Properties accessible.
        /// </summary>
        public string SecretName { get { return Options.SecretName; } }
        public string VaultName { get { return Options.VaultName; } }
        public IDictionary<string,string> Tags { get { return Options.Tags; } }

        /// <summary>
        /// Access token for the keyvault created on parsing.
        /// </summary>

        protected string AccessToken { get; set; }

        public bool IsPresent()
        {
            return !(string.IsNullOrEmpty(VaultName) || string.IsNullOrEmpty(SecretName));
        }

        public void SetSecret(string value, Dictionary<string, string> tags = null, string contentType = null, DateTime? notbefore = null)
        {
            var vaultUri = $"https://{Options.VaultName}.vault.azure.net";
            var keyvaultClient = new KeyVaultClient((_, __, c) => Task.FromResult(AccessToken));

            keyvaultClient.SetSecretAsync(vaultUri, Options.SecretName, value, tags, contentType, new SecretAttributes { NotBefore = notbefore }).GetAwaiter().GetResult();

        }

        public void OnConsoleParsing(Parser parser, string[] args, T options, PropertyInfo info)
        {
            AccessToken = EndpointProvider(options).GetToken("https://vault.azure.net");
        }
        
    }
}
