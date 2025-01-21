using System.Text.Json.Serialization;

namespace DynamicSettings.Models
{
    /// <summary>
    /// İşlem sonucunu ve varsa hata mesajını içeren jenerik sınıf
    /// </summary>
    /// <typeparam name="T">Sonuç içindeki veri tipi</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// İşlem başarılıysa veriyi döndürür. İşlem başarısızsa varsayılan(T) döndürür.
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; private set; }

        /// <summary>
        /// İşlem başarısızsa hata mesajını döndürür. İşlem başarılıysa null döndürür.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; private set; }

        private Result(bool isSuccess, T? data, string? error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        /// <param name="data">Sonuç içinde bulunacak veri</param>
        /// <returns>Yeni bir Result örneği başarılı sonuç için</returns>
        public static Result<T> Success(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Başarılı sonuç null değer içeremez");
            }
            return new Result<T>(true, data, null);
        }

        /// <summary>
        /// Hata sonucu oluşturur
        /// </summary>
        /// <param name="error">İşlem hatasını açıklayan hata mesajı</param>
        /// <returns>Yeni bir Result örneği hata sonucu için</returns>
        public static Result<T> Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentException("Hata mesajı boş olamaz", nameof(error));
            }
            return new Result<T>(false, default, error);
        }

        /// <summary>
        /// Başarılı sonucu farklı bir tipe dönüştürür.
        /// Hata durumunda mevcut hata mesajı korunur.
        /// </summary>
        /// <typeparam name="TNew">Yeni sonuç tipi</typeparam>
        /// <param name="mapper">Mevcut veriyi yeni tipe dönüştürmek için kullanılacak fonksiyon</param>
        /// <returns>Yeni bir Result örneği dönüştürülmüş veri veya hata mesajı için</returns>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            return IsSuccess
                ? Result<TNew>.Success(mapper(Data!))
                : Result<TNew>.Failure(Error!);
        }

        /// <summary>
        /// Sonuç durumuna göre ilgili işlemi çalıştırır
        /// </summary>
        /// <param name="onSuccess">İşlem başarılıysa çalıştırılacak eylem</param>
        /// <param name="onFailure">İşlem başarısızsa çalıştırılacak eylem</param>
        public void Match(Action<T> onSuccess, Action<string> onFailure)
        {
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));

            if (IsSuccess)
            {
                onSuccess(Data!);
            }
            else
            {
                onFailure(Error!);
            }
        }

        /// <summary>
        /// Sonucu string olarak döndürür
        /// </summary>
        public override string ToString()
        {
            return IsSuccess
                ? $"Success: {Data?.ToString()}"
                : $"Failure: {Error}";
        }
    }
} 