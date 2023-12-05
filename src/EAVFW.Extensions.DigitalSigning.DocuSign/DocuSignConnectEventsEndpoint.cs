using EAVFramework;
using EAVFramework.Endpoints.Results;
using EAVFramework.Hosting;
using EAVFW.Extensions.DigitalSigning.Actions;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using EAVFramework.Endpoints;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using System.Text.Json.Serialization;
using System.Linq;
using EAVFramework.Shared;
using System.Security.Claims;
using EAVFW.Extensions.Documents;
using Microsoft.EntityFrameworkCore;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{

    public partial class DocuSignEvent
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("retryCount")]
        public long RetryCount { get; set; }

        [JsonPropertyName("configurationId")]
        public long ConfigurationId { get; set; }

        [JsonPropertyName("generatedDateTime")]
        public DateTimeOffset GeneratedDateTime { get; set; }

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonPropertyName("accountId")]
        public Guid AccountId { get; set; }

        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("envelopeId")]
        public string EnvelopeId { get; set; }
    }
   
    [EntityInterface(EntityKey = "Signing Request")]
    [ConstraintMapping(EntityKey = "Digital Signing Provider", AttributeKey = "Status", ConstraintName = "TSigningProviderStatus")]
    public interface ISigningRequest<TSigningProvider, TSigningProviderStatus>
      where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>
      where TSigningProviderStatus : struct, IConvertible
    {
        public DateTime? CompletedOn {get;set;}
        public String EnvelopeId { get; set; }
        public TSigningProvider Provider { get; set; }
        public Guid? ProviderId { get; set; }

        public Guid? OwnerId { get; set; }
    }

    public class DocuSignConnectEventsEndpoint<TContext, TSigningRequest, TSigningProvider, TSigningProviderStatus,TDocument> : IEndpointHandler<TContext>
      where TContext : DynamicContext
      where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>
      where TSigningProviderStatus : struct, IConvertible
      where TSigningRequest : DynamicEntity, ISigningRequest<TSigningProvider, TSigningProviderStatus>
        where TDocument : DynamicEntity, IDocumentEntity


    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDocuSignClientFactory<TDocument> _docuSignClientFactory;
        private readonly EAVDBContext<TContext> _db;
        private readonly IDigitalSigningAuthContextProtector _dataProtectionProvider;


        public DocuSignConnectEventsEndpoint(
            IHttpClientFactory httpClientFactory,
            IDocuSignClientFactory<TDocument> docuSignClientFactory,
            EAVDBContext<TContext> db,
            IDigitalSigningAuthContextProtector dataProtectionProvider)

        {
            _httpClientFactory = httpClientFactory;
            _docuSignClientFactory = docuSignClientFactory;
            _db = db;
            _dataProtectionProvider = dataProtectionProvider;

        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var json = await context.Request.ReadFromJsonAsync<DocuSignEvent>();
            
            if(json.Event == "envelope-completed")
            {
                var signingRequest = await _db.Set<TSigningRequest>().Where(c=>c.EnvelopeId == json.Data.EnvelopeId)
                    .FirstOrDefaultAsync();

                signingRequest.CompletedOn = json.GeneratedDateTime.UtcDateTime;

              


                await _db.SaveChangesAsync(new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub",signingRequest.OwnerId.ToString())
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme)));

            }

            return new Status202AcceptedResult();
        }
    }
}