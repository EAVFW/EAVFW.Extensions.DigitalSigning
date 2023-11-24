using EAVFW.Extensions.DigitalSigning.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Workflows
{

    public class WizardDigitalSigningInitializeAuthContextWorkflow : Workflow
    {
        public WizardDigitalSigningInitializeAuthContextWorkflow()
        {
            Id = Guid.Parse("F6549001-4057-4B62-AE83-AF203BB43DED");
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
                    [DependencyInjection.WizardInitializeAuthContextAction] = new ActionMetadata
                    {
                        Type = DependencyInjection.WizardInitializeAuthContextAction,
                        Inputs =
                        {
                            
                            ["provider"]= "@triggerBody()?.data?.values?.provider",
                        }
                    },     
                    ["UpdateWizardContext"] = new ActionMetadata
                    {
                        RunAfter = new WorkflowRunAfterActions
                        {
                            [DependencyInjection.WizardInitializeAuthContextAction] = new []{"Succeded"}
                        },
                        Type = "UpdateWizardContext",
                        Inputs =
                        {    
                            ["values"] = new Dictionary<string,object>{
                                ["consentUrl"]=  $"@outputs('WizardInitializeAuthContextAction')?['body']['consentUrl']",
                                ["recordId"]=  $"@outputs('WizardInitializeAuthContextAction')?['body']['recordId']",
                            }
                        }

                    }
                }
            };


        }

    }
}
