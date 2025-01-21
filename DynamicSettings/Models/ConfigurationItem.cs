namespace DynamicSettings.Models
{
    public class ConfigurationItem
    {
        public string Path { get; set; }  // Örn: "ConnectionStrings:MongoConnection"
        public string Key { get; set; }   // Örn: "MongoConnection"
        public string Value { get; set; }  // Değer
        public Dictionary<string, ConfigurationItem> Children { get; set; } = new();
        public bool IsLeaf => !Children.Any();
    }
}
