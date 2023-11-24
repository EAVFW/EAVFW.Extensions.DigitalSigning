using EAVFramework;
using EAVFramework.Endpoints;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using ExpressionEngine;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Core;
using static IdentityModel.OidcConstants;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public class WizardInitializeAuthContextAction<TContext,TSigningProvider, TSigningProviderStatus> : IActionImplementation
        where TContext:DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>,new()
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly IOptions<DigitalSigningOptions> options;
        private readonly EAVDBContext<TContext> db;
        private readonly IDataProtectionProvider dataProtection;
        private readonly Base64UrlEncoder base64UrlEncoder;
        private readonly IEnumerable<SigningProviderType> providerTypes;

        public WizardInitializeAuthContextAction(
            IOptions<DigitalSigningOptions> options, 
            EAVDBContext<TContext> db,
            IDataProtectionProvider dataProtection , 
            Base64UrlEncoder base64UrlEncoder,
            IEnumerable<SigningProviderType> providerTypes)
        {
            this.options = options;
            this.db = db;
            this.dataProtection = dataProtection;
            this.base64UrlEncoder = base64UrlEncoder ?? throw new ArgumentNullException(nameof(base64UrlEncoder));
            this.providerTypes = providerTypes;
        }

        public async ValueTask<object> ExecuteAsync(IRunContext context, IWorkflow workflow, IAction action)
        {
            var providerType = providerTypes.FirstOrDefault();
            var configType = providerType.GetConfigurationType();


            var provider = action.Inputs["provider"] as ValueContainer;

            var raw = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(provider), configType);

            return await providerType.InitializeAuthContextAsync(this.options.Value,configType,raw,context.GetRunningPrincipal());
             
        }
    }
}
