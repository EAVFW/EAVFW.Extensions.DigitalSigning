using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EAVFW.Extensions.DigitalSigning.OpenXML;
using EAVFW.Extensions.Manifest.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DigitalSigning.DocuSign
{
    public class DocuSignFieldManager
    {
        private readonly OpenXMLService _openXMLService;
        private readonly ISchemaNameManager _schemaNameManager;

        public DocuSignFieldManager(OpenXMLService openXMLService, ISchemaNameManager schemaNameManager)
        {
            _openXMLService = openXMLService;
            _schemaNameManager = schemaNameManager;
        }
        public bool IsTagValidDocuSignFieldType(string tagValue, out DocuSignField? fieldtype)
        {
            fieldtype = null;
            // Check if the tagValue starts with "InputType:" and if the suffix matches any of the enum values
            if (string.IsNullOrEmpty(tagValue) || !tagValue.StartsWith("DocuSign:")) return false;

            var inputTypeName = tagValue["DocuSign:".Length..];
            if (Enum.TryParse<DocuSignField>(inputTypeName, out var type))
            {
                fieldtype = type;
                return true;
            }
            return false;
        }

        public List<DocusignControlElement> ExtractDocusignElements(WordprocessingDocument document)
        {
            var controlElements = new List<DocusignControlElement>();
            var contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>();

            foreach (var control in contentControls)
            {
                var tagElement = control.Descendants<Tag>().FirstOrDefault();

                if (tagElement != null)
                {
                    var tagValue = tagElement.Val?.ToString();

                    if (IsTagValidDocuSignFieldType(tagValue, out var type))
                    {
                        var sdtProperties = control.GetFirstChild<SdtProperties>();
                        if (sdtProperties != null)
                        {
                            // We only want one of each controlElement pr. title.
                            // This is a way to populate multiple fields later on in the program by using the title property as the identifier.
                            if (!controlElements.Any(ce =>  ce.SchemaName == _schemaNameManager.ToSchemaName( _openXMLService.GetSdtAliasValue(sdtProperties))))
                            {
                                var displayName = _openXMLService.GetSdtAliasValue(sdtProperties);
                                var schemaName = _schemaNameManager.ToSchemaName(displayName);
                               
                                controlElements.Add(
                                    new DocusignControlElement
                                    {
                                        Id = _openXMLService.GetSdtId(sdtProperties) ?? 0,
                                        DisplayName = displayName,
                                        SchemaName = schemaName,
                                        LogicalName = schemaName.ToLower(),
                                        Field = tagValue["DocuSign:".Length..]
                                    }
                                );
                            }
                        }
                    }
                }
            }

            return controlElements;
        }

        public async Task<DocuSignFieldMetadata> MakeFieldControlsTransparent(byte[] compressedDocByteArray)
        {
            using var loiDocument = await _openXMLService.OpenWordProcessingDocument(compressedDocByteArray, compressed: true);

            var docusignElements = ExtractDocusignElements(loiDocument);
            var list = new List<DocuSignFieldTemplate>();
            foreach (var documentControlElement in docusignElements)
            {
                var match = loiDocument.MainDocumentPart.Document.Descendants<SdtElement>()
                   .Where(c => _openXMLService.GetSdtId(c.GetFirstChild<SdtProperties>()) == documentControlElement.Id)
                   .FirstOrDefault();

                if (match != null)
                {

                    var props = match.GetFirstChild<SdtProperties>();
                    var tagElement = props.GetFirstChild<Tag>();
                    var tagValue = tagElement.Val?.Value;

                    if (IsTagValidDocuSignFieldType(tagElement.Val?.Value, out var fieldType))
                    {
                        var field = tagValue["DocuSign:".Length..];


                        var content = match.GetFirstChild<SdtContentRun>();

                        content.RemoveAllChildren();

                        Run formattedRun = new Run();
                        RunProperties runPro = new RunProperties();
                        // RunFonts runFont = new RunFonts() { Ascii = "Cambria(Headings)", HighAnsi = "Cambria(Headings)" };
                        //  Bold bold = new Bold();
                        Text text = new Text($"[DocuSign_{field}]");
                        Color color = new Color() { Val = "ffffff" };
                        //   runPro.Append(runFont);
                        //   runPro.Append(bold);
                        runPro.Append(color);
                        runPro.Append(text);
                        formattedRun.Append(runPro);


                        content.AddChild(formattedRun);

                        list.Add(new DocuSignFieldTemplate { Field = fieldType.Value, AnchorText = $"[DocuSign_{field}]" });
                    }
                }

            }

            loiDocument.Save();
            return new DocuSignFieldMetadata { Document = await _openXMLService.CompressDocumentAsync(loiDocument), Fields = list.ToArray() };
        }
    }
}