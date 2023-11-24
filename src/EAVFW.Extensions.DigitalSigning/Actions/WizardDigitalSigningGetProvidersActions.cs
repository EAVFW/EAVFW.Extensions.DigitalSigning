using EAVFramework;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public class WizardDigitalSigningGetProvidersActions<TContext, TSigningProvider, TSigningProviderStatus> : IActionImplementation
        where TContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly IEnumerable<SigningProviderType> providerTypes;

        public WizardDigitalSigningGetProvidersActions(IEnumerable<SigningProviderType> providerTypes)
        {
            this.providerTypes = providerTypes;
        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {


            return new
            {
                providers = providerTypes.Select(x=>new { name = x.ProviderName, configuration = x.GetConfigurationSchema() }).ToArray()
            
            };
        }
    }
}
