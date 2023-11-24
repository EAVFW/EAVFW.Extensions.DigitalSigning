using System;
using System.Collections.Generic;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Workflows
{
    public class WizardDigitalSigningInitializeWorkflow : Workflow
    {
        public WizardDigitalSigningInitializeWorkflow()
        {
            Id = Guid.Parse("F6349001-2057-3B62-AE83-AF203BB43DED");
            Version = "1.0.0";
            Manifest = new WorkflowManifest
            {
                Triggers =
                {
                    ["Trigger"] = new TriggerMetadata
                    {
                        Type = "Manual",
                        Inputs =
                        {

                        }
                    }
                },
                Actions =
                {
                    [DependencyInjection.WizardDigitalSigningGetProvidersActions] = new ActionMetadata
                    {
                        Type = DependencyInjection.WizardDigitalSigningGetProvidersActions,
                        Inputs =
                        {
                               
                        }
                    },
                    ["UpdateWizardContext"] = new ActionMetadata
                    {
                        RunAfter = new WorkflowRunAfterActions
                        {
                            [DependencyInjection.WizardDigitalSigningGetProvidersActions] = new []{"Succeded"}
                        },
                        Type = "UpdateWizardContext",
                        Inputs =
                        {
                            ["values"] = new Dictionary<string,object>{
                                ["providers"]=  $"@outputs('{DependencyInjection.WizardDigitalSigningGetProvidersActions}')?['body']['providers']",                                
                            }
                        }

                    }
                }
            };


        }

    }
}
