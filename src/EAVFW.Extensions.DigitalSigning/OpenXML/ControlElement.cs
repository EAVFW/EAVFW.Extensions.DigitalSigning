
namespace EAVFW.Extensions.DigitalSigning.OpenXML
{
    public class ControlElement : IControlElement
    {
        public int Id { get; set; }
        //public string Title { get; set; }
        public string InputType { get; set; }
        public string Placeholder { get; set; }


        public string LogicalName { get; set; }
        public string SchemaName { get; set; }

        public string DisplayName { get; set; }
        public string[] Tags { get; internal set; }

        public string Source { get; set; }
        public string SourceField { get; set; }

        public override string ToString()
        {
            return $"{{Id:{Id}, DisplayName:{DisplayName}, InputType:{InputType}, Placeholder:{Placeholder}}}";
        }
    }
}
