using System.ComponentModel.DataAnnotations;
using DynamicSettings.Models;
namespace DynamicSettings.Services.Interfaces
{
    /// <summary>
    /// Dinamik konfigürasyon ayarlarının yönetimi için servis arayüzü
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Tüm konfigürasyon değerlerini hiyerarşik bir ağaç yapısında getirir.
        /// Sadece test ortamında kullanılabilir.
        /// </summary>
        /// <returns>A Result containing the configuration tree if successful, or an error message if failed.</returns>
        Task<Result<ConfigurationTree>> GetConfigurationsAsync();

        /// <summary>
        /// Test ortamında belirtilen konfigürasyon değerini günceller.
        /// </summary>
        /// <param name="path">Konfigürasyon yolu ("section:subsection:key" formatında)</param>
        /// <param name="value">Ayarlanacak yeni değer</param>
        /// <returns>A Result containing the updated configuration item if successful, or an error message if failed.</returns>
        Task<Result<ConfigurationItem>> UpdateConfigurationAsync(
            [Required(ErrorMessage = "Konfigürasyon yolu zorunludur")] string path,
            [Required(ErrorMessage = "Konfigürasyon değeri zorunludur")] string value);

        /// <summary>
        /// Belirtilen yoldaki konfigürasyon değerini getirir
        /// </summary>
        /// <param name="path">Konfigürasyon yolu ("section:subsection:key" formatında)</param>
        Task<Result<ConfigurationItem>> GetConfigurationByPathAsync(
            [Required(ErrorMessage = "Konfigürasyon yolu zorunludur")] string path);

        /// <summary>
        /// Birden fazla konfigürasyon değerini tek seferde günceller
        /// </summary>
        /// <param name="updates">Güncellenecek konfigürasyon değerleri listesi</param>
        Task<Result<BulkUpdateResult>> BulkUpdateConfigurationsAsync(
            [Required(ErrorMessage = "Güncellenecek konfigürasyon listesi zorunludur")] 
            IEnumerable<ConfigurationUpdate> updates);
    }
}
