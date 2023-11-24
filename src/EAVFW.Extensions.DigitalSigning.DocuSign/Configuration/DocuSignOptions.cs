namespace EAVFW.Extensions.DigitalSigning.DocuSign.Configuration
{
    public class DocuSignRSAPair
    {
        public string Public { get; set; }
        public string Private { get; set; }
    }

    public class DocuSignOptions
    {
        public string IntegrationKey { get; set; }
        public string Secret { get; set; }
        public DocuSignRSAPair RSA { get; set; }
        public string BaseUrl { get;  set; }
    }
}
