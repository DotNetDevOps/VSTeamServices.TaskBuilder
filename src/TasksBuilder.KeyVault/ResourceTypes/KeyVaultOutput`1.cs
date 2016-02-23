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
        public KeyVaultOutput()
        {

        }

        public KeyVaultOutput(Func<T, ServiceEndpoint> endPointProvider)
        {
            EndpointProvider = endPointProvider;
        }
        public override KeyVaultOptions Options
        {
            get; set;
        }

        protected string AccessToken { get; set; }

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

        public bool IsPresent()
        {
            return !(string.IsNullOrEmpty(Options.VaultName) || string.IsNullOrEmpty(Options.SecretName));
        }


    }
}
