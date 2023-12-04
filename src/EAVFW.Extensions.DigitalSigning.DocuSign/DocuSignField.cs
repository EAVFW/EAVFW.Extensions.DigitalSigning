using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using System.Runtime.Serialization;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public enum DocuSignField
    {
        [EnumMember(Value = "DateSigned")]
        DateSigned,
        [EnumMember(Value = "SignHere")]
        SignHere

    }
}