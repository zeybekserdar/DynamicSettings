namespace DynamicSettings.Constants
{
    public static class ConfigurationConstants
    {
        /// <summary>
        /// Güncellenmesi yasak olan konfigürasyon yolları
        /// </summary>
        public static readonly IReadOnlyList<string> RestrictedPaths = new[]
        {
            "ConnectionStrings",
            "Authentication",
            "Security"
        };

        /// <summary>
        /// Görüntülenmesi yasak olan konfigürasyon yolları
        /// </summary>
        public static readonly IReadOnlyList<string> HiddenPaths = new[]
        {
            "Secrets",
            "ConnectionStrings",
            "ApiKeys",
            "Credentials",
            "PrivateKeys",
            "Tokens"
        };

        /// <summary>
        /// Konfigürasyon değişiklik log dosyası
        /// </summary>
        public const string ConfigChangeLogFile = "config-changes.log";
    }
}
