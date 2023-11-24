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

        public WizardDigitalSigningMakeActiveAction(EAVDBContext<TContext> db)
        {
            this._db = db;
        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {
            var recordid = Guid.Parse(action.Inputs["providerid"].ToString());
            var provider = await _db.Set<TSigningProvider>().FindAsync(recordid);

            provider.Status = (TSigningProviderStatus)Enum.ToObject(typeof(TSigningProviderStatus), Constants.SigningProviderReady);


            await _db.SaveChangesAsync(context.GetRunningPrincipal());
            return null;
        }
    }
}
