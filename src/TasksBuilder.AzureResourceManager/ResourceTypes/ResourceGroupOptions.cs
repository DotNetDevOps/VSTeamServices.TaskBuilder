
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


        [VisibleRule("CreateTemplatesOnly = false")]
        // [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Display(Description = "Resource Group for deployment", GroupName = "TemplateDeploymentOptions")]
        [Option("ResourceGroup")]
        public string ResourceGroup { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("ResourceGroupLocation")]
        [Display(ResourceType = typeof(AzureLocation), GroupName = "TemplateDeploymentOptions", Name = "Resource Group Location", Description = "Location to use if resource group do not exists")]
        public string ResourceGroupLocation { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("DeploymentName")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "The deployment name for the ARM deployment, if not provied a hash of input parameter names is used.", Name = "Deployment Name")]
        public string DeploymentName { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("CreateResourceGroup", DefaultValue = true)]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Location to use if resource group do not exists", Name = "Create Resource Group")]
        public bool CreateResourceGroup { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Display(ResourceType = typeof(Tags),
            Description = "Tags, seperate tags with comma and key:value with semicolon.",
            GroupName = "TemplateDeploymentOptions",
            Name = "Resource Group Tags",
            ShortName = "ResourceDeploymentTags")]
        public Dictionary<string, string> Tags { get; set; }

        [Option("CreateTemplatesOnly")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Create Template Files only but do not deploy", Name = "Create Templates Only")]
        public bool CreateTemplatesOnly { get; set; }

        [VisibleRule("CreateTemplatesOnly = true")]
        [Option("TemplateOutputPath")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "TemplateOutputPath", Name = "TemplateOutputPath")]
        public string TemplateOutputPath { get; set; }

        [VisibleRule("CreateTemplatesOnly = true")]
        [Option("TemplateParameterOutputPath")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "TemplateParameterOutputPath", Name = "TemplateParameterOutputPath")]
        public string TemplateParameterOutputPath { get; set; }

    }
}
