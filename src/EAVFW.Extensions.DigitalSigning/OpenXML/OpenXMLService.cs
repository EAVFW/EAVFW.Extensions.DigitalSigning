
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;

namespace EAVFW.Extensions.DigitalSigning.OpenXML
{
    public class OpenXMLService
    {
        public string GetSdtTitle(SdtProperties props)
        {

            var sdtAlias = props.GetFirstChild<SdtAlias>();
            if (sdtAlias != null)
            {
                return Regex.Replace(sdtAlias.Val.Value.ToLower(), @"[^a-zA-Z0-9]", "");
            }

            return null;
        }

        public bool IsMultilineControl(SdtProperties props)
        {
            var contentText = props.GetFirstChild<SdtContentText>();
            if (contentText?.MultiLine != null)
            {
                return OnOffValue.ToBoolean(contentText.MultiLine);
            }


            return false;

        }
        public int? GetSdtId(SdtProperties props)
        {
            var sdtId = props.GetFirstChild<SdtId>();
            if (sdtId != null && sdtId.Val.HasValue)
            {
                return sdtId.Val.Value;
            }

            return null;
        }

        public async Task<WordprocessingDocument> OpenWordProcessingDocument(byte[] data, bool compressed)
        {
            if (compressed)
            {
                var inputMemStream = new MemoryStream(data);
                var outputMemStream = new MemoryStream();
                var gz = new GZipStream(inputMemStream, CompressionMode.Decompress);
                await gz.CopyToAsync(outputMemStream);
                outputMemStream.Seek(0, SeekOrigin.Begin);
                return WordprocessingDocument.Open(outputMemStream, true);
            }
            else
            {
                var docStream = new MemoryStream(data);
                return WordprocessingDocument.Open(docStream, true);
            }
        }

        public async ValueTask<byte[]> CompressDocumentAsync(WordprocessingDocument doc)
        {
            using var compressedStream = new MemoryStream();

            using (var gz = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                using var uncompressedStream = new MemoryStream();
                doc.Clone(uncompressedStream);
                uncompressedStream.Seek(0, SeekOrigin.Begin);

                await uncompressedStream.CopyToAsync(gz);
            }

            return compressedStream.ToArray();
        }

        public bool IsMatch(SdtElement cc, IControlElement controlElement)
        {
            var sdtProperties = cc.GetFirstChild<SdtProperties>();
            var controlTitle = GetSdtTitle(sdtProperties);
            var isMatch = string.Equals(controlTitle, controlElement.Title, StringComparison.OrdinalIgnoreCase);
            //if (!isMatch)
            //{
            //    Console.WriteLine("Match: " + isMatch + "  ccTitle: " + controlTitle + "   ceTitle: " + controlElement.Title);
            //}
            return isMatch;
        }
    }
}
