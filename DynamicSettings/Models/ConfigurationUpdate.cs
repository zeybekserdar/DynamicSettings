using System.ComponentModel.DataAnnotations;

namespace DynamicSettings.Models
{
    /// <summary>
    /// Toplu güncelleme için konfigürasyon değişikliği modeli
    /// </summary>
    public class ConfigurationUpdate
    {
        /// <summary>
        /// Konfigürasyon yolu ("section:subsection:key" formatında)
        /// </summary>
        [Required(ErrorMessage = "Konfigürasyon yolu zorunludur")]
        public string Path { get; set; }

        /// <summary>
        /// Ayarlanacak yeni değer
        /// </summary>
        [Required(ErrorMessage = "Konfigürasyon değeri zorunludur")]
        public string Value { get; set; }
    }
} 