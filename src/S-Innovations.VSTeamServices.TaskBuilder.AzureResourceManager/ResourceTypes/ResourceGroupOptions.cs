﻿
namespace SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager.ResourceTypes
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using Attributes;
    using CommandLine;
    using TaskBuilder.ResourceTypes;

    [Group(DisplayName = "Deployment", isExpanded = true, Name = "TemplateDeploymentOptions")]
    public class ResourceGroupOptions
    {
        public class ConnectedServiceRelation : PropertyRelation<ResourceGroupOptions, ServiceEndpoint>
        {
            public ConnectedServiceRelation()
                : base(@class => @class.ConnectedServiceName)
            {

            }
        }


        public ResourceGroupOptions()
        {
            Tags = new Dictionary<string, string>();
        }

        [Option("CreateTemplatesOnly")]
        [Required]
        [Display(GroupName = "TemplateDeploymentOptions", Order = 999, Description = "Create Template Files only but do not deploy", Name = "Create Templates Only")]
        public bool CreateTemplatesOnly { get; set; }


        [VisibleRule("CreateTemplatesOnly = false")]
        [Required]
        [Display(Name = "Service Principal", GroupName = "TemplateDeploymentOptions", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }
        
        [ResourceGrounPicker(typeof(ConnectedServiceRelation))]
        [VisibleRule("CreateTemplatesOnly = false")]
        [Required]
        [Display(Description = "Provide the name of the resource group.", GroupName = "TemplateDeploymentOptions")]
        [Option("ResourceGroup")]
        public string ResourceGroup { get; set; }

        [LocationPickerAttribute(typeof(ConnectedServiceRelation))]
        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("ResourceGroupLocation")]
        [Required]
        [Display(ResourceType = typeof(AzureLocation), GroupName = "TemplateDeploymentOptions", Name = "Resource Group Location", Description = "Location to use if resource group do not exists")]
        public string ResourceGroupLocation { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("DeploymentName")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "The deployment name for the ARM deployment, if not provied a hash of input parameter names is used.", Name = "Deployment Name")]
        public string DeploymentName { get; set; }


        [DefaultValue(true)]
        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("AppendTimeStamp")]
        [Required]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Append a timespam to the deployment name", Name = "Append Timestamp")]
        public bool AppendTimeStamp { get; set; }

        [DefaultValue(true)]
        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("CreateResourceGroup")]
        [Required]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Create the resource group if it do not exists", Name = "Create Resource Group")]
        public bool CreateResourceGroup { get; set; }

        [VisibleRule("CreateTemplatesOnly = false")]
        [Display(ResourceType = typeof(Tags),
            Description = "Tags, seperate tags with comma and key:value with semicolon.",
            GroupName = "TemplateDeploymentOptions",
            Name = "Resource Group Tags",
            ShortName = "ResourceDeploymentTags")]
        public Dictionary<string, string> Tags { get; set; }

     
        [VisibleRule("CreateTemplatesOnly = true")]
        [Option("TemplateOutputPath")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "TemplateOutputPath", Name = "TemplateOutputPath")]
        public string TemplateOutputPath { get; set; }

        [VisibleRule("CreateTemplatesOnly = true")]
        [Option("TemplateParameterOutputPath")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "TemplateParameterOutputPath", Name = "TemplateParameterOutputPath")]
        public string TemplateParameterOutputPath { get; set; }


        [DefaultValue(true)]
        [VisibleRule("CreateTemplatesOnly = false")]
        [Option("WaitForDeploymentCompletion")]
        [Display(GroupName = "TemplateDeploymentOptions", Description = "Wait for the ARM deployment to complate before continuing", Name = "Wait for deployment")]
        public bool WaitForDeploymentCompletion { get; set; }

    }
}
