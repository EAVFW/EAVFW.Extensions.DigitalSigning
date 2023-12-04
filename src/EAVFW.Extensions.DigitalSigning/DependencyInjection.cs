using EAVFramework;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using EAVFW.Extensions.DigitalSigning.Actions;
using EAVFW.Extensions.DigitalSigning.OpenXML;
using EAVFW.Extensions.DigitalSigning.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using WorkflowEngine.Core;

namespace EAVFW.Extensions.DigitalSigning
{
    public static class Constants
    {
        public const int SigningProviderInitializing = 0;

        public const int SigningProviderInitialized = 10;

        public const int SigningProviderReady = 50;
    }
    public static class DependencyInjection
    {
        public const string WizardDigitalSigningValidateConsentAction = "WizardDigitalSigningValidateConsentAction";
        public const string WizardInitializeAuthContextAction = "WizardInitializeAuthContextAction";
        public const string WizardDigitalSigningMakeActiveAction = "WizardDigitalSigningMakeActiveAction";
        public const string WizardDigitalSigningGetProvidersActions = "WizardDigitalSigningGetProvidersActions";

        public static IServiceCollection AddDigitalSigning<TContext, TSigningProvider, TSigningProviderStatus>(this IServiceCollection services)
            where TContext : DynamicContext
            where TSigningProvider : DynamicEntity, ISigningProvider<TSigningProviderStatus>, new()
            where TSigningProviderStatus : struct, IConvertible
        {
            services.AddScoped<OpenXMLService>();
            services.AddWorkflow<WizardDigitalSigningInitializeWorkflow>();
            services.AddAction<WizardDigitalSigningGetProvidersActions<TContext, TSigningProvider, TSigningProviderStatus>>(WizardDigitalSigningGetProvidersActions);

            services.AddWorkflow<WizardDigitalSigningInitializeAuthContextWorkflow>();
            services.AddAction<WizardInitializeAuthContextAction<TContext, TSigningProvider, TSigningProviderStatus>>(WizardInitializeAuthContextAction);

            services.AddWorkflow<WizardDigitalSigningValidateConsentWorkflow>();
            services.AddAction<WizardDigitalSigningValidateConsentAction<TContext, TSigningProvider, TSigningProviderStatus>>(WizardDigitalSigningValidateConsentAction);

            services.AddWorkflow<WizardDigitalSigningMakeActiveWorkflow>();
            services.AddAction<WizardDigitalSigningMakeActiveAction<TContext, TSigningProvider, TSigningProviderStatus>>(WizardDigitalSigningMakeActiveAction);

            services.AddScoped<DigitalSigningAuthContextProtector<TContext, TSigningProvider, TSigningProviderStatus>>();
            services.AddScoped<IDigitalSigningAuthContextProtector, DigitalSigningAuthContextProtector<TContext, TSigningProvider, TSigningProviderStatus>>();
            services.AddOptions<DigitalSigningOptions>().Configure<IConfiguration>((o, c) => c.GetSection("DigitalSigning").Bind(o));

            return services;

        }

      
    }
}