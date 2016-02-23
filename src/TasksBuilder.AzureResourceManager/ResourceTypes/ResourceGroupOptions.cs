
namespace SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Attributes;
    using CommandLine;
    using TasksBuilder.ResourceTypes;

    [Group(DisplayName = "Deployment", isExpanded = true, Name = "TemplateDeploymentOptions")]
    public class ResourceGroupOptions
    {
        public ResourceGroupOptions()
        {
            Tags = new Dictionary<string, string>();
        }
        [Required]
        [Display(Description = "Resource Group for deployment", GroupName = "TemplateDeploymentOptions")]
        [Option("ResourceGroup")]
        public string ResourceGroup { get; set; }

        [Option("ResourceGroupLocation")]
        [Display(ResourceType = typeof(AzureLocation), GroupName = "TemplateDeploymentOptions", Name = "Resource Group Location", Description = "Location to use if resource group do not exists")]
        public string ResourceGroupLocation { get; set; }

        [Option("DeploymentName")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "The deployment name for the ARM deployment, if not provied a hash of input parameter names is used.", Name = "Deployment Name")]
        public string DeploymentName { get; set; }

        [Option("CreateResourceGroup", DefaultValue = true)]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Location to use if resource group do not exists", Name = "Create Resource Group")]
        public bool CreateResourceGroup { get; set; }


        [Display(ResourceType = typeof(Tags),
            Description = "Tags, seperate tags with comma and key:value with semicolon.",
            GroupName = "TemplateDeploymentOptions",
            Name = "Resource Group Tags",
            ShortName = "ResourceDeploymentTags")]
        public Dictionary<string, string> Tags { get; set; }


    }
}
