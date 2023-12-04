namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public class DocuSignFieldMetadata
    {
        public byte[] Document { get; set; }
        public DocuSignFieldTemplate[] Fields { get; set; }
    }
}