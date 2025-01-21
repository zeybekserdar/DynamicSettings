namespace DynamicSettings.Models
{
    /// <summary>
    /// Toplu güncelleme sonucu
    /// </summary>
    public class BulkUpdateResult
    {
        /// <summary>
        /// Başarılı olan güncellemeler
        /// </summary>
        public IReadOnlyList<ConfigurationItem> SuccessfulUpdates { get; set; }

        /// <summary>
        /// Başarısız olan güncellemeler ve hata mesajları
        /// </summary>
        public IReadOnlyList<FailedUpdate> FailedUpdates { get; set; }

        /// <summary>
        /// Toplam güncelleme sayısı
        /// </summary>
        public int TotalCount => (SuccessfulUpdates?.Count ?? 0) + (FailedUpdates?.Count ?? 0);

        /// <summary>
        /// Başarılı güncelleme sayısı
        /// </summary>
        public int SuccessCount => SuccessfulUpdates?.Count ?? 0;

        /// <summary>
        /// Başarısız güncelleme sayısı
        /// </summary>
        public int FailureCount => FailedUpdates?.Count ?? 0;
    }

    /// <summary>
    /// Başarısız güncelleme detayı
    /// </summary>
    public class FailedUpdate
    {
        /// <summary>
        /// Güncellenmeye çalışılan konfigürasyon
        /// </summary>
        public ConfigurationUpdate Update { get; set; }

        /// <summary>
        /// Hata mesajı
        /// </summary>
        public string ErrorMessage { get; set; }
    }
} 