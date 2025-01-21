namespace DynamicSettings.Models
{
    public class ConfigurationTree
    {
        public Dictionary<string, ConfigurationItem> Items { get; set; } = new();

        public void AddItem(string path, string value)
        {
            var segments = path.Split(':');
            var current = Items;
            var currentPath = "";

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}:{segment}";

                if (!current.ContainsKey(segment))
                {
                    current[segment] = new ConfigurationItem
                    {
                        Key = segment,
                        Path = currentPath,
                        Value = i == segments.Length - 1 ? value : null
                    };
                }

                current = current[segment].Children;
            }
        }
    }
}
