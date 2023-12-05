namespace EAVFW.Extensions.DigitalSigning.OpenXML
{
    public class DocusignControlElement : IControlElement
    {
        public int Id { get; set; }
        
       
        public string Field { get; set; }
        public string LogicalName { get; set; }
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
    }
}
