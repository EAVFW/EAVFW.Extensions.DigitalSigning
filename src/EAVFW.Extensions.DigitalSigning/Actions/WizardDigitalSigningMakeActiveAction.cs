using EAVFramework;
using EAVFramework.Endpoints;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public class WizardDigitalSigningMakeActiveAction<TContext, TSigningProvider, TSigningProviderStatus> : IActionImplementation
        where TContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly EAVDBContext<TContext> _db;
        private readonly IEnumerable<SigningProviderType> _providerTypes;

        public WizardDigitalSigningMakeActiveAction(EAVDBContext<TContext> db, IEnumerable<SigningProviderType> providerTypes)
        {
            this._db = db;
            _providerTypes = providerTypes;
        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {
            var recordid = Guid.Parse(action.Inputs["providerid"].ToString());
            var provider = await _db.Set<TSigningProvider>().FindAsync(recordid);

            provider.Status = (TSigningProviderStatus)Enum.ToObject(typeof(TSigningProviderStatus), Constants.SigningProviderReady);


            var providerType = _providerTypes.FirstOrDefault(x => string.Equals( x.ProviderName , provider.ProviderName,StringComparison.OrdinalIgnoreCase));

            await providerType.ActivateProviderAsync(recordid, action.Inputs["accountid"].ToString(),context.GetRunningPrincipal() );



            await _db.SaveChangesAsync(context.GetRunningPrincipal());
            return null;
        }
    }
}
