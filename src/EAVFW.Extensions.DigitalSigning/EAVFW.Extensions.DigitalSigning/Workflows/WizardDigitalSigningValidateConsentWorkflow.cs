using EAVFW.Extensions.DigitalSigning.Actions;
using System;
using System.Collections.Generic;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Workflows
{
    public class WizardDigitalSigningValidateConsentWorkflow : Workflow
    {
        public WizardDigitalSigningValidateConsentWorkflow()
        {
            Id = Guid.Parse("F6549001-3057-4B62-AE83-AF203BB43DED");
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
                    [DependencyInjection.WizardDigitalSigningValidateConsentAction] = new ActionMetadata
                    {
                        Type = DependencyInjection.WizardDigitalSigningValidateConsentAction,
                        Inputs =
                        {

                            ["providerid"]= "@triggerBody()?.data?.values?.recordId",
                        }
                    },                     
                    ["UpdateWizardContext"] = new ActionMetadata
                    {
                        RunAfter = new WorkflowRunAfterActions
                        {
                            [DependencyInjection.WizardDigitalSigningValidateConsentAction] = new []{"Succeded"}
                        },
                        Type = "UpdateWizardContext",
                        Inputs =
                        {
                            ["values"] = new Dictionary<string,object>{
                                ["accounts"]=  $"@outputs('{DependencyInjection.WizardDigitalSigningValidateConsentAction}')?['body']['accounts']",                              
                            }
                        }

                    }
                }
            };
        }
    }
}
