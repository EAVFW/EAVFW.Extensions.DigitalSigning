using EAVFramework.Shared;
using EAVFW.Extensions.DigitalSigning.Actions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DigitalSigning.Abstractions
{
    [EntityInterface(EntityKey = "Digital Signing Provider")]
    [ConstraintMapping(AttributeKey = "Status", ConstraintName = "TSigningProviderStatus")]
    public interface ISigningProvider<TSigningProviderStatus>
         where TSigningProviderStatus : struct, IConvertible
    {
        public Guid Id { get; set; }
        public String ProviderName { get; set; }
        public String AuthContext { get; set; }

        public Guid? OwnerId { get; set; }

        public TSigningProviderStatus? Status { get; set; }

    }
    public class SingingProviderConfiguration
    {
        [DataMember(Name ="schema")]
        [JsonPropertyName("schema")]
        [JsonProperty("schema")]
        public object Schema { get; set; }

        [DataMember(Name ="uiSchema")]
        [JsonPropertyName("uiSchema")]
        [JsonProperty("uiSchema")]
        public object UISchema { get; set; }

        
    }
    public abstract class SigningProviderType
    {
        public abstract string ProviderName { get; }

        public virtual SingingProviderConfiguration GetConfigurationSchema()
        {
            return new SingingProviderConfiguration { };
        }

        public abstract Type GetConfigurationType();
        public ValueTask<object> InitializeAuthContextAsync(DigitalSigningOptions options,Type configType, object config, ClaimsPrincipal user)
        {
            var method = typeof(SigningProviderType<>).MakeGenericType(configType)
                .GetMethod(nameof(InitializeAuthContextAsync), new[] { typeof(DigitalSigningOptions), configType,typeof(ClaimsPrincipal) });

            return (ValueTask<object>)method.Invoke(this, new object[] { options, config, user });
        }

    }
    public abstract class SigningProviderType<TConfiguration> : SigningProviderType
    {
        public abstract ValueTask<object> InitializeAuthContextAsync(DigitalSigningOptions options, TConfiguration configuration, ClaimsPrincipal user);

        public override Type GetConfigurationType() => typeof(TConfiguration);

    }
}
