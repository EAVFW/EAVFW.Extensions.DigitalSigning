using EAVFramework;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using IdentityModel.Client;
using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EAVFW.Extensions.DigitalSigning.Actions
{



    public class WizardDigitalSigningValidateConsentAction<TContext, TSigningProvider, TSigningProviderStatus> : IActionImplementation
        where TContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
        where TSigningProviderStatus : struct, IConvertible
    {
     

        private readonly DigitalSigningAuthContextProtector<TContext,TSigningProvider, TSigningProviderStatus> digitalSigningAuthContextProtector;

        public WizardDigitalSigningValidateConsentAction(DigitalSigningAuthContextProtector<TContext, TSigningProvider, TSigningProviderStatus> digitalSigningAuthContextProtector)
        {
            this.digitalSigningAuthContextProtector = digitalSigningAuthContextProtector;
        }
        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {
            var recordid = Guid.Parse(action.Inputs["providerid"].ToString());

            var ctx = await digitalSigningAuthContextProtector.UnprotectAuthContext(recordid);

            var userinfo = ctx.UserInfoResponse;
            var claims = JsonDocument.Parse(userinfo).RootElement.ToClaims();

            var accounts = claims.Where(c => c.Type == "accounts").Select(c=> JsonSerializer.Deserialize< DocusignAccount>( c.Value)).ToArray();

            return new { accounts };
          


        }
    }
}
