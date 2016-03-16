using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace SInnovations.VSTeamServices.TasksBuilder.KeyVault.ResourceTypes
{
    public class KeyVaultOptions
    {
        private readonly KeyVaultOutput options;
        public KeyVaultOptions(KeyVaultOutput options){
            this.options = options;
        }
        [ArmResourceIdPicker("Microsoft.KeyVault/vaults", "2015-06-01")]
        [Display(Description = "The keyvault namespace", Name = "KeyVault Name", GroupName = "KeyVault")]
        [Required]
        [Option("KeyVaultName")]
        public string VaultName { get { return options.VaultName; } set { options.VaultName = value.StartsWith("/subscriptions") ? value.Split('/').Last() :value; } }

        [ArmResourceProviderPicker("$(KeyVaultName)","secrets", "2015-06-01")]
        [Display(Description = "The keyvault secret name to store value in", Name = "Secret Name", GroupName = "KeyVault")]
        [Required]
        [Option("SecretName")]
        public string SecretName { get { return options.SecretName; } set { options.SecretName = value.StartsWith("/subscriptions")? value.Substring(value.IndexOf("secrets/")+ "secrets/".Length).Split('/').First():value; } }


        [Display(ResourceType = typeof(Tags),
          Description = "Tags, seperate tags with comma and key:value with semicolon.",
          GroupName = "KeyVault",
          Name = "Secret Tags",
          ShortName = "KeyVaultSecretTags")]
        public Dictionary<string, string> Tags { get; set; }

       
    }

}
