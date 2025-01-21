namespace DynamicSettings.Models
{
    public class UpdateConfigurationRequest
    {
        public string Path { get; set; }  // Örn: "ConnectionStrings:MongoConnection"
        public string Value { get; set; }
    }
}
