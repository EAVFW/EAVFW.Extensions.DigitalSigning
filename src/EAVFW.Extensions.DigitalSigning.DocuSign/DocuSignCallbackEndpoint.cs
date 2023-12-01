using EAVFramework;
using EAVFramework.Endpoints.Results;
using EAVFramework.Hosting;
using EAVFW.Extensions.DigitalSigning.Actions;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using EAVFramework.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using System.Text.Json;
using System.Security.Claims;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public class DocuSignCallbackEndpoint<TContext,TSigningProvider, TSigningProviderStatus> : IEndpointHandler<TContext>
        where TContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EAVDBContext<TContext> _db;
        private readonly IDigitalSigningAuthContextProtector _dataProtectionProvider;
        private readonly Base64UrlEncoder base64UrlEncoder;

        public DocuSignCallbackEndpoint(
            IHttpClientFactory httpClientFactory, 
            EAVDBContext<TContext> db,
            IDigitalSigningAuthContextProtector dataProtectionProvider,
            Base64UrlEncoder base64UrlEncoder)
        {
            this._httpClientFactory = httpClientFactory;
            this._db = db;
            this._dataProtectionProvider = dataProtectionProvider;
            this.base64UrlEncoder = base64UrlEncoder;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
           

            var m = new IdentityModel.Client.AuthorizeResponse(context.Request.QueryString.Value);
             

            var provider = await _db.Set<TSigningProvider>().FindAsync(Guid.Parse(m.State));
            var http = _httpClientFactory.CreateClient();

            var authContext = _dataProtectionProvider.UnprotectAuthContext(provider.AuthContext); 

            var response = await http.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = $"{authContext.BaseUrl}/oauth/token",

                ClientId = authContext.ClientId,
                ClientSecret = authContext.ClientSecret,

                Code = m.Code,
                RedirectUri = authContext.RedirectUrl,


            });

            if (response.IsError)
            {
                throw new InvalidOperationException(response.Error);
            }
            authContext.TokenResponse = response.Raw;

           

            var userinfo = await http.GetUserInfoAsync(new UserInfoRequest
            {
                Address = $"{authContext.BaseUrl.Trim('/')}/oauth/userinfo",
                Token = response.AccessToken
            });


            authContext.UserInfoResponse = userinfo.Raw;

            //{ "sub":"03ee9fd7-866a-4a9f-89ac-5a05638803be","name":"Poul Kjeldager","given_name":"Poul","family_name":"Kjeldager","created":"2023-05-15T01:11:27","email":"info@kjeldager.com","accounts":[{ "account_id":"bf48a901-43f9-4a99-a538-2eea9cbeb91e","is_default":true,"account_name":"Kjeldager Drift ApS","base_uri":"https://demo.docusign.net"}]}


            provider.AuthContext = _dataProtectionProvider.ProtectAuthContext(authContext);

            provider.Status = (TSigningProviderStatus)Enum.ToObject(typeof(TSigningProviderStatus), Constants.SigningProviderInitialized);

            await _db.SaveChangesAsync(new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub",provider.OwnerId.ToString())
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme)));

            return new DataEndpointResult(new { status = "ok"});
        }
    }
}