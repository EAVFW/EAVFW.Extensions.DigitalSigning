using EAVFramework;
using EAVFW.Extensions.DigitalSigning.Actions;
using EAVFW.Extensions.DigitalSigning.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Sprache;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using System.Xml.Linq;
using IdentityModel;
using Microsoft.OData.UriParser;
using System;
using EAVFW.Extensions.DigitalSigning.DocuSign.Configuration;
using EAVFW.Extensions.DigitalSigning.Services;
using EAVFW.Extensions.DigitalSigning.DocuSign.Services;
using System.Collections.Concurrent;
using System.Net.Http;
using EAVFW.Extensions.Documents;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public interface IDocuSignClientFactory<TDocument>
         where TDocument : DynamicEntity, IDocumentEntity
    {
        public DocusignClient<TDocument> CreateClient(Guid signingProviderID);
    }
    public class DocuSignContext
    {
        public static HttpRequestOptionsKey<DocuSignContext> DigitalSigningContextKey = new HttpRequestOptionsKey<DocuSignContext>("DigitalSigningContext");

        public Guid SigningProviderID { get; set; }
    }
    public class DocuSignClientFactory<TDynamicContext, TSigningProvider, TSigningProviderStatus,TDocument> : IDocuSignClientFactory<TDocument>
        where TDynamicContext : DynamicContext
        where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>
        where TSigningProviderStatus : struct, IConvertible
        where TDocument : DynamicEntity, IDocumentEntity
    {
        private readonly IServiceProvider serviceProvider;
      //  private ConcurrentDictionary<Guid, IServiceScope> _clients = new ConcurrentDictionary<Guid, IServiceScope>();

        public DocuSignClientFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public DocusignClient<TDocument> CreateClient(Guid signingProviderID)
        {
            var ctx = this.serviceProvider.GetService<DocuSignContext>();
            ctx.SigningProviderID = signingProviderID;

            return this.serviceProvider.GetRequiredService<DocusignClient<TDocument>>();
        }

       
    }

    public static class DependencyInjection
    {

        public static IServiceCollection AddDocuSignSigningProvider<TDynamicContext,TSigningProvider, TSigningProviderStatus,TDocument>(this IServiceCollection services)
            where TDynamicContext : DynamicContext                         
            where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
            where TSigningProviderStatus : struct, IConvertible
            where TDocument : DynamicEntity, IDocumentEntity
        {

            services.AddScoped<SigningProviderType, DocuSignProviderType<TDynamicContext,TSigningProvider,TSigningProviderStatus>>();
            services.AddOptions<DocuSignOptions>().Configure<IConfiguration>((options, config) => config.GetSection("DigitalSigning:DocuSign").Bind(options));
            services.AddSingleton<Base64UrlEncoder>();
            services.AddEndpoint<DocuSignCallbackEndpoint<TDynamicContext,TSigningProvider, TSigningProviderStatus>, TDynamicContext>("DocusignCallback", "/callbacks/docusign", "GET")
                .IgnoreRoutePrefix();


            services.AddScoped<DocuSignContext>();
            services.AddScoped<DocusignHttpClientHandler>();
            services.AddScoped<IDocuSignClientFactory<TDocument>, DocuSignClientFactory<TDynamicContext, TSigningProvider, TSigningProviderStatus,TDocument>>();
            services.AddHttpClient<DocusignClient<TDocument>>()
                .ConfigureHttpClient(http =>
                {


                }).AddHttpMessageHandler< DocusignHttpClientHandler>();


            return services;

        }
    }
}