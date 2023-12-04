using DocuSign.eSign.Model;
using EAVFramework;
using EAVFW.Extensions.DigitalSigning.Actions;
using EAVFW.Extensions.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocuDocument = DocuSign.eSign.Model.Document;

namespace EAVFW.Extensions.DigitalSigning.DocuSign.Services
{
    
    public class DocusignClient<TDocument> 
        where TDocument : DynamicEntity, IDocumentEntity
    {
        
        private readonly HttpClient _httpClient;
        private readonly DocuSignContext _digitalSigningContext;
        private readonly IDigitalSigningAuthContextProtector _digitalSigningAuthContextProtector;

        

       // public string AccountId { get; } = "bf48a901-43f9-4a99-a538-2eea9cbeb91e";

        public DocusignClient(HttpClient httpClient, DocuSignContext digitalSigningContext)
        {
            _httpClient = httpClient;
            _digitalSigningContext = digitalSigningContext;
           
        }

        public Task<HttpResponseMessage> PostAsJsonAsync(string url, object payload)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json")
            };
            req.Options.Set(DocuSignContext.DigitalSigningContextKey, _digitalSigningContext);

            return this._httpClient.SendAsync(req);
        }
        public async Task<EnvelopeDefinition> MakeEnvelopeAsync(string clientuserid, string signerEmail, string signerName, TDocument document, Tabs tabs)
        {
            // byte[] buffer = System.IO.File.ReadAllBytes("C:\\Users\\PoulKjeldagerSorense\\Downloads\\testdoc.pdf");


            async Task<byte[]> GetData()
            {

                if (document.Compressed ?? false)
                {
                    var data = new MemoryStream();

                    using (var stream = new GZipStream(new MemoryStream(document.Data), CompressionMode.Decompress))
                    {

                        await stream.CopyToAsync(data);

                    }

                    return data.ToArray();
                }

                return document.Data;
            }


            var data = await GetData();

            EnvelopeDefinition envelopeDefinition = new EnvelopeDefinition()
            { 
            };
            envelopeDefinition.EmailSubject = "Please sign this document";
            var doc1 = new DocuDocument();

            string doc1b64 = Convert.ToBase64String(data);

            doc1.DocumentBase64 = doc1b64;
            doc1.Name = document.Name;// "Lorem Ipsum"; // can be different from actual file name
            doc1.FileExtension = Path.GetExtension(document.Name).Trim('.');// "pdf";
            doc1.DocumentId = "1";// document.Id.ToString();

            // The order in the docs array determines the order in the envelope
            envelopeDefinition.Documents = new List<DocuDocument> { doc1 };

            // Create a signer recipient to sign the document, identified by name and email
            // We set the clientUserId to enable embedded signing for the recipient
            // We're setting the parameters via the object creation
            Signer signer1 = new Signer
            {
                Email = signerEmail,
                Name = signerName,
                ClientUserId = clientuserid,
                RecipientId = "1"
            };

            
            signer1.Tabs = tabs;

            // Add the recipient to the envelope object
            Recipients recipients = new Recipients
            {
                Signers = new List<Signer> { signer1 }
            };
            envelopeDefinition.Recipients = recipients;

            // Request that the envelope be sent by setting |status| to "sent".
            // To request that the envelope be created as a draft, set to "created"
            envelopeDefinition.Status = "sent";

            return envelopeDefinition;
        }

        public RecipientViewRequest MakeRecipientViewRequest(string clientuserid, string signerEmail, string signerName, string returnUrl)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global

            RecipientViewRequest viewRequest = new RecipientViewRequest();
            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            viewRequest.ReturnUrl = returnUrl;

            // How has your app authenticated the user? In addition to your app's
            // authentication, you can include authenticate steps from DocuSign.
            // Eg, SMS authentication
            viewRequest.AuthenticationMethod = "none";

            // Recipient information must match embedded recipient info
            // we used to create the envelope.
            viewRequest.Email = signerEmail;
            viewRequest.UserName = signerName;
            viewRequest.ClientUserId = clientuserid;

            // DocuSign recommends that you redirect to DocuSign for the
            // Signing Ceremony. There are multiple ways to save state.
            // To maintain your application's session, use the pingUrl
            // parameter. It causes the DocuSign Signing Ceremony web page
            // (not the DocuSign server) to send pings via AJAX to your
            // app,
            //  viewRequest.PingFrequency = "600"; // seconds
            // NOTE: The pings will only be sent if the pingUrl is an https address
            //  viewRequest.PingUrl = dsPingUrl; // optional setting

            return viewRequest;
        }

    }
}