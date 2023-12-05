
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using EAVFW.Extensions.Manifest.SDK;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EAVFW.Extensions.DigitalSigning.OpenXML
{
    public enum InputType
    {
        [EnumMember(Value = Constants.InputTypes.Text)]
        Text,
        [EnumMember(Value = Constants.InputTypes.MultilineText)]
        MultilineText,
        [EnumMember(Value = Constants.InputTypes.RTF)]
        RTF,       
        [EnumMember(Value = Constants.InputTypes.Date)]
        Date,       
    }

    public class OpenXMLService
    {
        private readonly ISchemaNameManager _schemaNameManager;

        public OpenXMLService(ISchemaNameManager schemaNameManager)
        {
            _schemaNameManager = schemaNameManager;
        }
        public string GetSchemaName(SdtProperties props)
        {
            return _schemaNameManager.ToSchemaName(GetSdtAliasValue(props));
        }
        public string GetSdtAliasValue(SdtProperties props)
        {

            var sdtAlias = props.GetFirstChild<SdtAlias>();
            if (sdtAlias != null)
            {
                return sdtAlias.Val?.Value;
              //  return Regex.Replace(sdtAlias.Val.Value.ToLower(), @"[^a-zA-Z0-9]", "");
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

        public bool IsMatch(SdtElement source, IControlElement target)
        {
            var sdtProperties = source.GetFirstChild<SdtProperties>();
            var controlTitle = _schemaNameManager.ToSchemaName(GetSdtAliasValue(sdtProperties));
            var isMatch = string.Equals(controlTitle, target.SchemaName, StringComparison.OrdinalIgnoreCase);          
            return isMatch;
        }

        private bool IsTagValidInputType(string tagValue, out InputType? inputType)
        {
            inputType = null;
            // Check if the tagValue starts with "InputType:" and if the suffix matches any of the enum values
            if (string.IsNullOrEmpty(tagValue) || !tagValue.StartsWith("InputType:")) return false;

            var inputTypeName = tagValue["InputType:".Length..];
            if( Enum.TryParse<InputType>(inputTypeName, out var inputtype))
            {
                inputType = inputtype;
                return true;
            }
            return false;
        }
        public List<ControlElement> ExtractControlElements(WordprocessingDocument document)
        {
            var controlElements = new List<ControlElement>();
            var contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>();

            foreach (var control in contentControls)
            {
                var tagElement = control.Descendants<Tag>().FirstOrDefault();

                if (tagElement != null)
                {
                    var tags = tagElement.Val?.ToString().Split(',');
                    var tagValue = tags.FirstOrDefault();

                    if (IsTagValidInputType(tagValue, out var inputType))
                    {
                        var sdtProperties = control.GetFirstChild<SdtProperties>();
                        if (sdtProperties != null)
                        {
                            // We only want one of each controlElement pr. title.
                            // This is a way to populate multiple fields later on in the program by using the title property as the identifier.
                            if (!controlElements.Any(ce => ce.SchemaName == _schemaNameManager.ToSchemaName( GetSdtAliasValue(sdtProperties))))
                            {
                                var title = GetSdtAliasValue(sdtProperties);
                                var schemaName = _schemaNameManager.ToSchemaName(title);
                                var isMultiline = IsMultilineControl(sdtProperties);
                                
                                controlElements.Add(
                                    new ControlElement
                                    {
                                        Id = GetSdtId(sdtProperties) ?? 0,
                                        DisplayName = title,
                                        SchemaName = schemaName,
                                        LogicalName = schemaName.ToLower(),
                                        Placeholder = string.Join("", control.Descendants<Text>().Select(t => t.Text)).Trim(),
                                        InputType = inputType == InputType.Text && isMultiline ? "MultilineText" : tagValue["InputType:".Length..],
                                        Tags = tags.Skip(1).ToArray()
                                    }
                                );
                            }
                        }
                    }
                }
            }

            return controlElements;
        }


    }
}
