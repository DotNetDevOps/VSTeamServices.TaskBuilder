using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.KeyVault;
using Newtonsoft.Json.Linq;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

namespace SInnovations.VSTeamServices.TasksBuilder.KeyVault.ResourceTypes
{
    public static class KeyVaultOutputExtensions
    {
        public static bool IsConfigured(this KeyVaultOutput kvo)
        {
            if (kvo == null)
                return false;

            return !string.IsNullOrEmpty(kvo.VaultName) && !string.IsNullOrEmpty(kvo.SecretName);
        }
    
    }
    public class KeyVaultOutput : DefaultConsoleReader<KeyVaultOptions>
    {
        public string VaultName { get; set; }

        public string SecretName { get; set; }
    }
    public class KeyVaultOutput<T> : KeyVaultOutput, IConsoleReader<T>
    {
        private Lazy<KeyVaultClient> _vaultClient;

        //public Func<T, ServiceEndpoint> EndpointProvider { get; private set; }

        /// <summary>
        /// Parameter less constructor for generating Options.
        /// </summary>
        public KeyVaultOutput()
        {
            Options = new KeyVaultOptions(this);
        }

        ///// <summary>
        ///// Runtime constructor
        ///// </summary>
        ///// <param name="endPointProvider"></param>
        //public KeyVaultOutput(Func<T, ServiceEndpoint> endPointProvider) : this()
        //{
        //    EndpointProvider = endPointProvider;
        //}

        public Dictionary<string,string> Tags { get { return Options.Tags; } }

        /// <summary>
        /// Access token for the keyvault created on parsing.
        /// </summary>

        protected string AccessToken { get; set; }

      

        public void SetSecret(string value, Dictionary<string, string> tags = null, string contentType = null, DateTime? notbefore = null, bool? enabled=null,DateTime? expires=null)
        {
            var vaultUri = $"https://{VaultName}.vault.azure.net";
             KeyVaultClient.SetSecretAsync(vaultUri, Options.SecretName, value, tags, contentType, new SecretAttributes { NotBefore = notbefore, Enabled=enabled, Expires=expires}).GetAwaiter().GetResult();

        }
        public async Task SetSecretIfNotExistAsync(string value, Dictionary<string, string> tags = null, string contentType = null, DateTime? notbefore = null, bool? enabled = null, DateTime? expires = null)
        {
            var vaultUri = $"https://{VaultName}.vault.azure.net";
            var secrets = await KeyVaultClient.GetSecretsAsync(vaultUri);

            if (secrets.Value == null || !secrets.Value.Any(s => s.Id == vaultUri + "/secrets/" + SecretName))
            {
                await KeyVaultClient.SetSecretAsync(vaultUri, Options.SecretName, value, tags, contentType, new SecretAttributes { NotBefore = notbefore, Enabled = enabled, Expires = expires });
            }
        }
        public async Task SaveCertificateAsync(byte[] cert, string password, Dictionary<string, string> tags = null, TimeSpan? saveIfCurrentExpiresWithin=null)
        {
            var certBase64 = Convert.ToBase64String(cert);
            var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(new JObject(
                new JProperty("data", certBase64),
                new JProperty("dataType", "pfx"),
                new JProperty("password", password)
                ).ToString()));

            var x509Certificate = new X509Certificate2(cert, password, X509KeyStorageFlags.Exportable);
            tags["thumbprint"]= x509Certificate.Thumbprint;
            tags["friendlyName"]= x509Certificate.FriendlyName;

            var vaultUri = $"https://{VaultName}.vault.azure.net";
            var secrets = await KeyVaultClient.GetSecretsAsync(vaultUri);

            if (saveIfCurrentExpiresWithin == null || secrets.Value == null || !secrets.Value.Any(s => s.Id == vaultUri + "/secrets/" + SecretName))
            {
                await KeyVaultClient.SetSecretAsync(vaultUri, SecretName, value, tags, "application/x-pkcs12", new SecretAttributes { NotBefore = x509Certificate.NotBefore, Expires = x509Certificate.NotAfter });
            }
            else
            {
                var last = secrets.Value.Single(s => s.Id == vaultUri + "/secrets/" + SecretName);
                if(( last.Attributes.Expires - DateTime.UtcNow) < saveIfCurrentExpiresWithin)
                {
                    await KeyVaultClient.SetSecretAsync(vaultUri, SecretName, value, tags, "application/x-pkcs12", new SecretAttributes { NotBefore = x509Certificate.NotBefore, Expires = x509Certificate.NotAfter });
                }

            }
        }
        public KeyVaultClient KeyVaultClient { get { return _vaultClient.Value; } }

        public void OnConsoleParsing(Parser parser, string[] args, T options, PropertyInfo info)
        {
            var cr = info.GetCustomAttribute<ConnectedServiceRelationAttribute>();
            var propertyRelation = (PropertyRelation)Activator.CreateInstance(cr.ConnectedService);
            var connectedService = (ServiceEndpoint)propertyRelation.GetProperty(options) ;
            AccessToken = connectedService.GetToken("https://vault.azure.net");

            _vaultClient = new Lazy<KeyVaultClient>(() => new KeyVaultClient((_, __, c) => Task.FromResult(AccessToken)));
        }

       
    }
}
