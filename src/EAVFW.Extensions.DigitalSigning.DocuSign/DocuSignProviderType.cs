using EAVFW.Extensions.DigitalSigning.Abstractions;
using System;
using Microsoft.Extensions.Options;
using EAVFW.Extensions.DigitalSigning.DocuSign.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using EAVFW.Extensions.DigitalSigning.Actions;
using IdentityModel.Client;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using static IdentityModel.OidcConstants;
using EAVFramework;
using System.Text.Json;
using EAVFramework.Endpoints;
using System.Security.Claims;
using System.Linq;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{

    public class DocuSignProviderType<TContext, TSigningProvider, TSigningProviderStatus> : SigningProviderType<DocuSignOptions>
        where TContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly IOptions<DocuSignOptions> options;
        private readonly IDigitalSigningAuthContextProtector signingAuthContextProtector;
        private readonly EAVDBContext<TContext> db;

        public DocuSignProviderType(
            IOptions<DocuSignOptions> options,
            IDigitalSigningAuthContextProtector signingAuthContextProtector,
            EAVDBContext<TContext> db
            )
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.signingAuthContextProtector = signingAuthContextProtector;
            this.db = db;
        }
        public override string ProviderName => "DocuSign";

        public override async Task ActivateProviderAsync(Guid recordid, string accountid, ClaimsPrincipal user)
        {

            var authContext = await signingAuthContextProtector.UnprotectAuthContext(recordid);

            var userinfo = authContext.UserInfoResponse;
            var claims = JsonDocument.Parse(userinfo).RootElement.ToClaims();

            var accounts = claims.Where(c => c.Type == "accounts").Select(c => JsonSerializer.Deserialize<DocusignAccount>(c.Value)).ToArray();

            authContext.Account = accounts.FirstOrDefault(x => x.AccountId == accountid);

            await signingAuthContextProtector.ProtectAuthContextAsync(recordid, authContext);
        }

        public override SingingProviderConfiguration GetConfigurationSchema()
        {
            var props = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(options.Value.BaseUrl))
            {
                props.Add("baseurl", new
                {
                    title = "Docusign Base Url",
                    type = "string",
                    @default = "https://account-d.docusign.com/"
                });
            }
            if (string.IsNullOrEmpty(options.Value.IntegrationKey))
            {
                props.Add("integrationKey", new
                {
                    title = "Integration Key",
                    type = "string",
                    format = "password"
                });
            }
            if (string.IsNullOrEmpty(options.Value.Secret))
            {
                props.Add("secret", new
                {
                    title = "Integration Secret",
                    type = "string",
                    format = "password"
                });
            }


            return new SingingProviderConfiguration
            {
                
                Schema = new
                {
                    id = "docusign",
                    type = "object",
                    properties = props,
                    required = props.Keys
                }
            };
        }

        public override async ValueTask<object> InitializeAuthContextAsync(
            DigitalSigningOptions digitalSigningOptions,
            DocuSignOptions config,
            ClaimsPrincipal user
            )
        {
            var baseUrl = config.BaseUrl ?? options.Value.BaseUrl;
            var integrationKey = config.IntegrationKey ?? options.Value.IntegrationKey;
            var secret = config.Secret ?? options.Value.Secret;
            var rsa = config.RSA?.Private ?? options.Value.RSA.Private;
            var ru = new RequestUrl($"{baseUrl.Trim('/')}/oauth/auth");

            //  var state = $"baseurl={baseurl}&integrationKey={integrationKey}&";
            var authcontext = new DigitalSigningContext { 
                BaseUrl = baseUrl, 
                ClientId = integrationKey,
                ClientSecret = secret,
                PrivateRSA = rsa,
                RedirectUrl = $"{digitalSigningOptions.CallbackBaseUrl.Trim('/')}/callbacks/docusign" };
            var record = new TSigningProvider
            {
                ProviderName = ProviderName,
                AuthContext = signingAuthContextProtector.Protect(JsonSerializer.Serialize(authcontext)),
                Status = (TSigningProviderStatus)Enum.ToObject(typeof(TSigningProviderStatus), Constants.SigningProviderInitializing)
            };
            db.Add(record);

            await db.SaveChangesAsync(user);
             
            var consentUrl = ru.CreateAuthorizeUrl(integrationKey,
                 ResponseTypes.Code,
                 "impersonation",
                authcontext.RedirectUrl,
                 record.Id.ToString()
                 );

            //  var url =  IdentityModel.Client.AuthorizationCodeTokenRequest

            // var callback = ;

            //var consent = $"{baseurl.Trim('/')}/oauth/auth?response_type=code&scope=impersonation&client_id={integrationKey}&redirect_uri={callback}";

            return new { consentUrl = consentUrl, recordId = record.Id.ToString() };
        }
    }
}