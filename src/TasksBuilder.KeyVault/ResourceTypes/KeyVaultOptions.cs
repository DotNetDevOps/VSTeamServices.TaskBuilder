using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace SInnovations.VSTeamServices.TasksBuilder.KeyVault.ResourceTypes
{
    public class KeyVaultOptions
    {

        [Display(
            Description = "The keyvault namespace containing the certificate", Name = "KeyVault Name", GroupName = "KeyVault")]
        [Option("KeyVaultName")]
        public string VaultName { get; set; }
        [Display(Description = "The keyvault secret name to store value in", Name = "Secret Name", GroupName = "KeyVault")]
        [Option("SecretName")]
        public string SecretName { get; set; }


        [Display(ResourceType = typeof(Tags),
          Description = "Tags, seperate tags with comma and key:value with semicolon.",
          GroupName = "KeyVault",
          Name = "Secret Tags",
          ShortName = "KeyVaultSecretTags")]
        public Dictionary<string, string> Tags { get; set; }

    }

}
