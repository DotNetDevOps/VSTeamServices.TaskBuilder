using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.ResourceManager;
using SInnovations.Azure.ResourceManager.TemplateActions;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager;
using SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;

namespace SInnovations.VSTeamServices.TaskBuilder.KeyVault.ResourceTypes
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
    public class SecretAttribute : Attribute
    {
        //   public bool ListKeys { get; set; }
        //   public string ListKeysSelectToken { get; set; }
        public string ResourceIdOutput { get; set; }
        public string Value { get; set; }
        public Type ArmDeploymentRelation { get; set; }
    }

    public class KeyVaultOutput : DefaultConsoleReader<KeyVaultOptions>
    {
        public string VaultName { get; set; }

        public string SecretName { get; set; }
    }
    class AddVariable : ITemplateAction
    {
        private JToken jToken;
        private string variable;

        public AddVariable(string variable, JToken jToken)
        {
            this.variable = variable;
            this.jToken = jToken;
        }

        public Task TemplateActionAsync(JObject obj)
        {
            var variables = obj["variables"] as JObject;
            variables.Add(variable, jToken);

            return Task.FromResult(0);
        }
    }
    class AddParamter : ITemplateAction
    {
        private JToken jToken;
        private string variable;

        public AddParamter(string variable, JToken jToken)
        {
            this.variable = variable;
            this.jToken = jToken;
        }

        public Task TemplateActionAsync(JObject obj)
        {
            var variables = obj["parameters"] as JObject;
            variables.Add(variable, jToken);

            return Task.FromResult(0);
        }
    }
    public class SaveCertificateResult
    {
        public X509Certificate2 Certificate { get; set; }
        public bool Updated { get; set; }
        public string SecretUriWithVersion { get; set; }
    }
   
    public class KeyVaultOutput<T> : KeyVaultOutput, IConsoleReader<T>, IConsoleExecutor<T> where T : class, new()
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

        public Dictionary<string, string> Tags { get { return Options.Tags; } }

        /// <summary>
        /// Access token for the keyvault created on parsing.
        /// </summary>

        protected string AccessToken { get; set; }



        public void SetSecret(string value, Dictionary<string, string> tags = null, string contentType = null, DateTime? notbefore = null, bool? enabled = null, DateTime? expires = null)
        {
            var vaultUri = $"https://{VaultName}.vault.azure.net";
            KeyVaultClient.SetSecretAsync(vaultUri, Options.SecretName, value, tags, contentType, new SecretAttributes { NotBefore = notbefore, Enabled = enabled, Expires = expires }).GetAwaiter().GetResult();

        }
        public async Task SetSecretIfNotExistAsync(string value, Dictionary<string, string> tags = null, string contentType = null, DateTime? notbefore = null, bool? enabled = null, DateTime? expires = null)
        {
            var vaultUri = $"https://{VaultName}.vault.azure.net";
            var secrets = await KeyVaultClient.GetSecretsAsync(vaultUri);

            if (!secrets.Any(s => s.Id == vaultUri + "/secrets/" + SecretName))
            {
                await KeyVaultClient.SetSecretAsync(vaultUri, Options.SecretName, value, tags, contentType, new SecretAttributes { NotBefore = notbefore, Enabled = enabled, Expires = expires });
            }
        }

     

        public async Task<SaveCertificateResult> SaveCertificateAsync(Func<byte[]> certGenerator, string password, Dictionary<string, string> tags = null, TimeSpan? saveIfCurrentExpiresWithin = null, string secretContentType= "application/x-pkcs12")
        {

            var vaultUri = $"https://{VaultName}.vault.azure.net";
            var secrets = await KeyVaultClient.GetSecretsAsync(vaultUri);

            Func<Task<SaveCertificateResult>> setTagsAndReturn = async () =>
             {
                 var cert = certGenerator();
                 var certBase64 = Convert.ToBase64String(cert);
                 var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(new JObject(
                     new JProperty("data", certBase64),
                     new JProperty("dataType", "pfx"),
                     new JProperty("password", password)
                     ).ToString()));

                 var x509Certificate = new X509Certificate2(cert, password, X509KeyStorageFlags.Exportable);
                 tags["thumbprint"] = x509Certificate.Thumbprint;
                 tags["friendlyName"] = x509Certificate.FriendlyName;

                var secret = await KeyVaultClient.SetSecretAsync(vaultUri, SecretName, value, tags, secretContentType, new SecretAttributes { NotBefore = x509Certificate.NotBefore, Expires = x509Certificate.NotAfter });

                 return new SaveCertificateResult { Certificate = x509Certificate, Updated = true, SecretUriWithVersion = secret.SecretIdentifier.Identifier };

             };

            if (saveIfCurrentExpiresWithin == null || !secrets.Any(s => s.Id == vaultUri + "/secrets/" + SecretName))
            {
                var value = await setTagsAndReturn();
                return value;
            }
            else
            {
                var last = secrets.Single(s => s.Id == vaultUri + "/secrets/" + SecretName);
                if ((last.Attributes.Expires - DateTime.UtcNow) < saveIfCurrentExpiresWithin)
                {
                    return await setTagsAndReturn();
                }

                var lastSecret = await KeyVaultClient.GetSecretAsync(last.Identifier.Identifier);

                var obj = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(lastSecret.Value)));
                var cert = new X509Certificate2(Convert.FromBase64String(obj["data"].ToString()), obj["password"].ToString(), X509KeyStorageFlags.Exportable);

                return new SaveCertificateResult { Certificate = cert, Updated = false, SecretUriWithVersion = lastSecret.SecretIdentifier.Identifier };

            }
        }

         

        public Task<SaveCertificateResult> SaveCertificateAsync(byte[] cert, string password, Dictionary<string, string> tags = null, TimeSpan? saveIfCurrentExpiresWithin = null, string secretContentType = "application/x-pkcs12")
        {
            return this.SaveCertificateAsync(() => cert, password, tags, saveIfCurrentExpiresWithin,secretContentType);
        }
        

        public KeyVaultClient KeyVaultClient { get { return _vaultClient.Value; } }

        protected SecretAttribute[] SecretAttributes { get; set; }
        public void OnConsoleParsing(Parser parser, string[] args, T options, PropertyInfo info)
        {

            SecretAttributes = info.GetCustomAttributes<SecretAttribute>().ToArray();

            foreach (var secret in SecretAttributes)
            {
                //{
                //    var armDeployment = (Activator.CreateInstance(secret.ArmDeploymentRelation) as PropertyRelation<T, ArmTemplateDeployment<T>>).GetProperty(options);
                //    armDeployment.AfterLoad.Add(template=>
                //    {
                //       var resources= template.SelectToken("resources") as JArray;
                //        resources.Add(new ResourceSource("S-Innovations.VSTeamServices.TasksBuilder.KeyVault.secret.json", typeof(KeyVaultOutput<>).Assembly)
                //        {
                //            new ResourceParamterConstant("keyVaultName",VaultName),
                //            new ResourceParamterConstant("secretName",SecretName),
                //            new ResourceParamterConstant("secretValue",secret.Value)

                //        });
                //    });
            }


            var cr = info.GetCustomAttribute<ConnectedServiceRelationAttribute>();
            var propertyRelation = (PropertyRelation)Activator.CreateInstance(cr.ConnectedService);
            var connectedService = (ServiceEndpoint)propertyRelation.GetProperty(options);
            AccessToken = connectedService.GetToken("https://vault.azure.net");

            _vaultClient = new Lazy<KeyVaultClient>(() => new KeyVaultClient((_, __, c) => Task.FromResult(AccessToken)));
        }
        protected ServiceEndpoint ServiceEndpoint { get; set; }
        public void Execute(T options)
        {
            if (this.IsConfigured())
            {
                foreach (var secret in SecretAttributes)
                {
                    var armDeployment = (Activator.CreateInstance(secret.ArmDeploymentRelation) as PropertyRelation<T, ArmTemplateDeployment<T>>).GetProperty(options);

                    if (armDeployment.ResourceGroupOptions.CreateTemplatesOnly)
                    {
                        continue;
                    }

                    var secretTemplate = new ResourceSource("S-Innovations.VSTeamServices.TasksBuilder.KeyVault.secret.json", typeof(KeyVaultOutput<>).Assembly);

               

                    var secretParamters = new JObject(
                                            ResourceManagerHelper.CreateValue("keyVaultName", VaultName),
                                            ResourceManagerHelper.CreateValue("secretName", SecretName)
                                            );

                    if (!secret.Value.ToString().StartsWith("["))
                    {
                        secretParamters.Add(ResourceManagerHelper.CreateValue("secretValue", secret.Value));
                        secretTemplate.Add(new AddParamter("secretValue", new JObject(new JProperty("type", "securestring"))));
                    }
                    else
                    {
                        var parameters = new List<string>(Regex.Matches(secret.Value, @"parameters\('(.*?)'\)").OfType<Match>().Select(m => m.Groups[1].Value).Distinct());
                        var variables = new Queue<string>(Regex.Matches(secret.Value, @"variables\('(.*?)'\)").OfType<Match>().Select(m => m.Groups[1].Value).Distinct());
                        while (variables.Count > 0)
                        {
                            var variable = variables.Dequeue();
                            var value = armDeployment.Variables.SelectToken(variable).ToString();
                            secretTemplate.Add(new AddVariable(variable, value));

                            foreach (var newVar in Regex.Matches(value, @"variables\('(.*?)'\)").OfType<Match>().Select(m => m.Groups[1].Value).Distinct())
                                variables.Enqueue(newVar);

                            parameters.AddRange(Regex.Matches(value, @"parameters\('(.*?)'\)").OfType<Match>().Select(m => m.Groups[1].Value).Distinct());

                        }

                        foreach (var parameter in parameters.Distinct())
                        {
                            secretTemplate.Add(new AddParamter(parameter, armDeployment.Template.SelectToken("parameters."+parameter)));
                            secretParamters.Add(parameter, armDeployment.Parameters.SelectToken(parameter));
                        }

                        secretTemplate.Add(new ResourceParamterConstant("secretValue",secret.Value));
                    }



                    var endpoint = armDeployment.EndpointProvider(options);
                    var managemenetToken = endpoint.GetToken("https://management.azure.com/");
                    var ResourceGroupOptions = armDeployment.ResourceGroupOptions;
                    var result = ResourceManagerHelper.CreateTemplateDeploymentAsync(new ApplicationCredentials
                    {
                        AccessToken = managemenetToken,
                        SubscriptionId = endpoint.SubscriptionId,
                        TenantId = endpoint.TenantId,
                    }, ResourceGroupOptions.ResourceGroup,
                    SecretName + "-" + DateTimeOffset.UtcNow.ToString("MMdd-HHmmss"),
                    secretTemplate, secretParamters).Result;
                    
                    var secretUriWithVersion = JObject.FromObject(result.Properties.Outputs).SelectToken("secret.value.secretUriWithVersion");
                    Console.WriteLine(secretUriWithVersion);
                }
            }
        }
    }
}
