namespace EAVFW.Extensions.DigitalSigning.OpenXML
{
    public interface IControlElement
    {
        public int Id { get; set; }

        public string LogicalName { get; set; }
        public string SchemaName { get; set; }

        public string DisplayName { get; set; }
        //public string Title { get; set; }
       // public string Placeholder { get; set; }

    }
}
