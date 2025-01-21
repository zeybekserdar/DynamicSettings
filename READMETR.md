# Dynamic Settings API

Bu API, uygulama konfigürasyonlarını dinamik olarak yönetmenizi sağlayan bir servis sunar. Test ortamında çalışan uygulamanızın konfigürasyonlarını görüntüleyebilir ve güncelleyebilirsiniz.

## Özellikler

- 🔍 Tüm konfigürasyonları hiyerarşik yapıda görüntüleme
- 🎯 Belirli bir path'deki konfigürasyonu görüntüleme
- ✏️ Tekil konfigürasyon güncelleme
- 📦 Toplu konfigürasyon güncelleme
- 🔒 Hassas bilgileri içeren konfigürasyonları gizleme
- 🚫 Kritik konfigürasyonların güncellenmesini engelleme
- 📝 Konfigürasyon değişikliklerini loglama
- 🔄 Thread-safe operasyonlar

## API Endpoints

### Tüm Konfigürasyonları Getir

```http
GET /api/configuration
```

Tüm konfigürasyon değerlerini hiyerarşik bir ağaç yapısında döndürür.

### Belirli Bir Konfigürasyonu Getir

```http
GET /api/configuration/{path}
```

Örnek:
```http
GET /api/configuration/Logging:LogLevel:Default
```

### Konfigürasyon Güncelle

```http
PUT /api/configuration/{path}
Content-Type: application/json

"yeni_değer"
```

Örnek:
```http
PUT /api/configuration/Logging:LogLevel:Default
Content-Type: application/json

"Information"
```

### Toplu Güncelleme

```http
PUT /api/configuration/bulk
Content-Type: application/json

[
  {
    "path": "Logging:LogLevel:Default",
    "value": "Information"
  },
  {
    "path": "AllowedHosts",
    "value": "*"
  }
]
```

## Güvenlik Özellikleri

### Gizli Konfigürasyonlar

Aşağıdaki path'lerdeki konfigürasyonlar güvenlik nedeniyle görüntülenemez:

- Secrets
- ApiKeys
- Credentials
- PrivateKeys
- Tokens
- ConnectionStrings

### Kısıtlı Konfigürasyonlar

Aşağıdaki path'lerdeki konfigürasyonlar güncellenemez:

- ConnectionStrings
- Authentication
- Security

## Kurulum

1. Projeyi klonlayın
```bash
git clone [repo-url]
```

2. Bağımlılıkları yükleyin
```bash
dotnet restore
```

3. Uygulamayı çalıştırın
```bash
dotnet run
```

## Kullanım Örneği

```csharp
// Konfigürasyon servisi enjeksiyonu
public class SampleService
{
    private readonly IConfigurationService _configService;

    public SampleService(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task UpdateLogLevel()
    {
        var result = await _configService.UpdateConfigurationAsync(
            "Logging:LogLevel:Default", 
            "Debug"
        );

        if (result.IsSuccess)
        {
            // Güncelleme başarılı
            var updatedConfig = result.Data;
        }
        else
        {
            // Hata durumu
            var errorMessage = result.Error;
        }
    }
}
```

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakınız. 