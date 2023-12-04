using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public class DocuSignFieldTemplate
    {
        [JsonPropertyName("anchorText")]
        public string AnchorText { get; set; }
        [JsonPropertyName("field")]
        public DocuSignField Field { get; set; }
    }
}