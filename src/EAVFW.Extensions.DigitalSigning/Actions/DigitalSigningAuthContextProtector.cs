using EAVFramework;
using EAVFramework.Endpoints;
using EAVFW.Extensions.DigitalSigning.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public interface IDigitalSigningAuthContextProtector : IDataProtector
    {
        DigitalSigningContext UnprotectAuthContext(string authcontext);
        string ProtectAuthContext(DigitalSigningContext authcontext);
        Task<DigitalSigningContext> UnprotectAuthContext(Guid recordid);
        Task ProtectAuthContextAsync(Guid recordid, DigitalSigningContext authcontext);
    }
    public class DigitalSigningAuthContextProtector <TDynamicContext, TSigningProvider, TSigningProviderStatus>  : IDigitalSigningAuthContextProtector
        where TDynamicContext : DynamicContext
        where TSigningProvider: DynamicEntity, ISigningProvider<TSigningProviderStatus>
        where TSigningProviderStatus : struct, IConvertible
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly EAVDBContext<TDynamicContext> _db;

        public DigitalSigningAuthContextProtector(IDataProtectionProvider dataProtectionProvider, EAVDBContext<TDynamicContext> db)
        {
            this._dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
            this._db = db;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            return this._dataProtectionProvider.CreateProtector("DigitalSigning").CreateProtector(purpose);
        }

        public byte[] Protect(byte[] plaintext)
        {
            return this._dataProtectionProvider.CreateProtector("DigitalSigning").Protect(plaintext);
             
        }

        public string ProtectAuthContext(DigitalSigningContext authContext)
        {
            return this.Protect(JsonSerializer.Serialize(authContext));
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return this._dataProtectionProvider.CreateProtector("DigitalSigning").Unprotect(protectedData);
        }

        public DigitalSigningContext UnprotectAuthContext(string authcontext)
        {
            return JsonSerializer.Deserialize<DigitalSigningContext>(this.Unprotect(authcontext));
        }

        public async Task<DigitalSigningContext> UnprotectAuthContext(Guid recordid)
        {
            var provider = await _db.Set<TSigningProvider>().FindAsync(recordid);
          

            var authContext = JsonSerializer.Deserialize<DigitalSigningContext>(this.Unprotect(provider.AuthContext));
            return authContext;
        }
        public async Task ProtectAuthContextAsync(Guid recordid, DigitalSigningContext authContext)
        {
            var provider = await _db.Set<TSigningProvider>().FindAsync(recordid);

            provider.AuthContext = this.ProtectAuthContext(authContext);

          
        }
    }
}
