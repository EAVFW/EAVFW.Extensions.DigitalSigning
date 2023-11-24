using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Workflows
{
    
    public class WizardDigitalSigningMakeActiveWorkflow : Workflow
    {
        public WizardDigitalSigningMakeActiveWorkflow()
        {
            Id = Guid.Parse("F6549000-1057-4B62-AE83-AF203BB43DED");
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
                    [DependencyInjection.WizardDigitalSigningMakeActiveAction] = new ActionMetadata
                    {
                        Type = DependencyInjection.WizardDigitalSigningMakeActiveAction,
                        Inputs =
                        {

                            ["providerid"]= "@triggerBody()?.data?.values?.recordId",
                        }
                    }, 
                }
            };
        }
    }
}
