

namespace SInnovations.VSTeamServices.TasksBuilder.KeyVault.ResourceTypes
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using CommandLine;
    using SInnovations.VSTeamServices.TasksBuilder.Attributes;
    using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

    /// <summary>
    /// Parsing options for <see cref="KeyVaultOutput"/> that is used to generate input options in vsts task.
    /// </summary>
    public class KeyVaultOptions
    {
        private readonly KeyVaultOutput options;
        public KeyVaultOptions(KeyVaultOutput options){
            this.options = options;
        }
        
        [ArmResourceIdPicker("Microsoft.KeyVault/vaults", "2015-06-01")]
        [Display(Description = "The keyvault namespace", Name = "KeyVault Name")]
        [Option("KeyVaultName")]
        public string VaultName { get { return options.VaultName; } set { options.VaultName = value.StartsWith("/subscriptions") ? value.Split('/').Last() :value; } }

        [ArmResourceProviderPicker("$(KeyVaultName)","secrets", "2015-06-01")]
        [Display(Description = "The keyvault secret name to store value in", Name = "Secret Name")]
        [Option("SecretName")]
        public string SecretName { get { return options.SecretName; } set { options.SecretName = value.StartsWith("/subscriptions")? value.Substring(value.IndexOf("secrets/")+ "secrets/".Length).Split('/').First():value; } }

        [Display(ResourceType = typeof(Tags),
          Description = "Tags, seperate tags with comma and key:value with semicolon.",
          Name = "Secret Tags",
          ShortName = "KeyVaultSecretTags")]
        public Dictionary<string, string> Tags { get; set; }

       
    }

}
