using System.Text.Json;
using DynamicSettings.Models;
using DynamicSettings.Services.Interfaces;
using DynamicSettings.Constants;
using Microsoft.Extensions.Configuration;

namespace DynamicSettings.Services
{
    /// <summary>
    /// Uygulama içindeki dinamik konfigürasyon ayarlarını yöneten servis.
    /// Sadece test ortamında konfigürasyon değerlerini okuma ve güncelleme işlemlerini sağlar.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRoot _configRoot;
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger<ConfigurationService> _logger;
        
        // Sık erişilen ayar dosyası yolu için önbellek
        private readonly string _settingsFilePath;
        
        // Thread-safe dosya işlemleri için semaphore
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public ConfigurationService(
            IConfiguration configuration,
            IEnvironmentService environmentService,
            ILogger<ConfigurationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configRoot = (IConfigurationRoot)configuration;
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsFilePath = _environmentService.GetTestSettingsPath();
        }

        /// <summary>
        /// Tüm konfigürasyon değerlerini hiyerarşik bir ağaç yapısında getirir.
        /// Sadece test ortamında kullanılabilir.
        /// </summary>
        public async Task<Result<ConfigurationTree>> GetConfigurationsAsync()
        {
            if (!_environmentService.IsTestEnvironment())
            {
                _logger.LogWarning("Test ortamı dışında konfigürasyon görüntüleme denemesi");
                return Result<ConfigurationTree>.Failure("Konfigürasyon görüntüleme sadece test ortamında kullanılabilir");
            }

            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogError("Ayar dosyası bulunamadı: {Path}", _settingsFilePath);
                    return Result<ConfigurationTree>.Failure("appsettings.Development.json dosyası bulunamadı");
                }

                await using var fileStream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var jsonDoc = await JsonDocument.ParseAsync(fileStream);
                
                var tree = new ConfigurationTree();
                ProcessJsonElement(tree, string.Empty, jsonDoc.RootElement);

                return Result<ConfigurationTree>.Success(tree);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Konfigürasyon dosyası JSON format hatası");
                return Result<ConfigurationTree>.Failure("Konfigürasyon dosyası geçersiz JSON formatı içeriyor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Konfigürasyon okuma hatası");
                return Result<ConfigurationTree>.Failure($"Konfigürasyon okuma hatası: {ex.Message}");
            }
        }

        private void ProcessJsonElement(ConfigurationTree tree, string parentPath, JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var currentPath = string.IsNullOrEmpty(parentPath) 
                        ? property.Name 
                        : $"{parentPath}:{property.Name}";

                    // Gizli path kontrolü
                    if (!IsHiddenPath(currentPath))
                    {
                        ProcessJsonElement(tree, currentPath, property.Value);
                    }
                }
            }
            else
            {
                var value = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    JsonValueKind.Object => JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true }),
                    JsonValueKind.Array => JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true }),
                    _ => element.GetRawText()
                };

                if (!string.IsNullOrEmpty(value))
                {
                    tree.AddItem(parentPath, value);
                }
            }
        }

        /// <summary>
        /// Test ortamında belirtilen konfigürasyon değerini günceller.
        /// Thread-safe dosya işlemleri ve doğrulama kontrolleri içerir.
        /// </summary>
        public async Task<Result<ConfigurationItem>> UpdateConfigurationAsync(string path, string value)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Result<ConfigurationItem>.Failure("Configuration path cannot be empty");
            }

            if (!_environmentService.IsTestEnvironment())
            {
                _logger.LogWarning("Attempted to update configuration in non-test environment. Path: {Path}", path);
                return Result<ConfigurationItem>.Failure("Configuration updates are only allowed in development environment");
            }

            if (IsRestrictedPath(path))
            {
                _logger.LogWarning("Attempted to update restricted configuration path: {Path}", path);
                return Result<ConfigurationItem>.Failure($"Updating {path} is not allowed");
            }

            try
            {
                await _semaphore.WaitAsync();
                
                var result = await UpdateSettingsFileAsync(path, value);
                if (result.IsSuccess)
                {
                    _configRoot.Reload();
                    await LogConfigurationChangeAsync(path, value);
                }
                
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static bool IsRestrictedPath(string path) =>
            ConfigurationConstants.RestrictedPaths.Any(p => 
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        private static bool IsHiddenPath(string path) =>
            ConfigurationConstants.HiddenPaths.Any(p => 
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        private async Task<Result<ConfigurationItem>> UpdateSettingsFileAsync(string path, string value)
        {
            try 
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return Result<ConfigurationItem>.Failure("Test settings file not found");
                }

                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var jsonDict = ConvertJsonElementToDictionary(jsonDoc.RootElement);

                UpdateNestedValue(jsonDict, path.Split(':'), value);

                var newJson = JsonSerializer.Serialize(
                    jsonDict,
                    new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                await File.WriteAllTextAsync(_settingsFilePath, newJson);

                return Result<ConfigurationItem>.Success(new ConfigurationItem 
                { 
                    Path = path, 
                    Value = value,
                    Key = path.Split(':').Last()
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON format while updating configuration");
                return Result<ConfigurationItem>.Failure("Failed to parse configuration file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration. Path: {Path}, Value: {Value}", path, value);
                return Result<ConfigurationItem>.Failure($"Failed to update configuration: {ex.Message}");
            }
        }

        private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Object => ConvertJsonElementToDictionary(prop.Value),
                        JsonValueKind.Array => ConvertJsonArrayToList(prop.Value),
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => prop.Value.GetRawText()
                    };
                }
            }
            
            return dict;
        }

        private static List<object> ConvertJsonArrayToList(JsonElement element)
        {
            var list = new List<object>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(item.ValueKind switch
                {
                    JsonValueKind.Object => ConvertJsonElementToDictionary(item),
                    JsonValueKind.Array => ConvertJsonArrayToList(item),
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number => item.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => item.GetRawText()
                });
            }
            return list;
        }

        private void UpdateNestedValue(Dictionary<string, object> dict, string[] pathSegments, string value)
        {
            try
            {
                var current = dict;
                for (var i = 0; i < pathSegments.Length - 1; i++)
                {
                    var segment = pathSegments[i];
                    
                    if (!current.ContainsKey(segment))
                    {
                        current[segment] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    }
                    
                    if (current[segment] is not Dictionary<string, object> nestedDict)
                    {
                        nestedDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        current[segment] = nestedDict;
                    }
                    
                    current = nestedDict;
                }

                current[pathSegments.Last()] = value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateNestedValue. PathSegments: {PathSegments}, Value: {Value}", 
                    string.Join(":", pathSegments), value);
                throw;
            }
        }

        private async Task LogConfigurationChangeAsync(string path, string value)
        {
            try
            {
                var logMessage = $"Configuration changed - Path: {path}, Value: {value}, Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}, Environment: Test";
                var logPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    ConfigurationConstants.ConfigChangeLogFile);

                await File.AppendAllTextAsync(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log configuration change. Path: {Path}, Value: {Value}", path, value);
                // Don't throw as this is not critical for the operation
            }
        }

        public async Task<Result<ConfigurationItem>> GetConfigurationByPathAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Result<ConfigurationItem>.Failure("Konfigürasyon yolu boş olamaz");
            }

            if (!_environmentService.IsTestEnvironment())
            {
                _logger.LogWarning("Test ortamı dışında konfigürasyon görüntüleme denemesi. Path: {Path}", path);
                return Result<ConfigurationItem>.Failure("Konfigürasyon görüntüleme sadece test ortamında kullanılabilir");
            }

            if (IsHiddenPath(path))
            {
                _logger.LogWarning("Gizli konfigürasyon görüntüleme denemesi. Path: {Path}", path);
                return Result<ConfigurationItem>.Failure($"'{path}' yolundaki konfigürasyon görüntülenemez");
            }

            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogError("Ayar dosyası bulunamadı: {Path}", _settingsFilePath);
                    return Result<ConfigurationItem>.Failure("appsettings.Development.json dosyası bulunamadı");
                }

                await using var fileStream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var jsonDoc = await JsonDocument.ParseAsync(fileStream);

                var pathSegments = path.Split(':');
                var current = jsonDoc.RootElement;

                foreach (var segment in pathSegments)
                {
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                    {
                        return Result<ConfigurationItem>.Failure($"'{path}' yolunda konfigürasyon bulunamadı");
                    }
                }

                var value = current.ValueKind switch
                {
                    JsonValueKind.String => current.GetString(),
                    JsonValueKind.Number => current.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    JsonValueKind.Object => JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }),
                    JsonValueKind.Array => JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }),
                    _ => current.GetRawText()
                };

                if (value == null)
                {
                    return Result<ConfigurationItem>.Failure($"'{path}' yolundaki konfigürasyon değeri null");
                }

                return Result<ConfigurationItem>.Success(new ConfigurationItem
                {
                    Path = path,
                    Value = value,
                    Key = pathSegments.Last()
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Konfigürasyon dosyası JSON format hatası");
                return Result<ConfigurationItem>.Failure("Konfigürasyon dosyası geçersiz JSON formatı içeriyor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Konfigürasyon okuma hatası. Path: {Path}", path);
                return Result<ConfigurationItem>.Failure($"Konfigürasyon okuma hatası: {ex.Message}");
            }
        }

        public async Task<Result<BulkUpdateResult>> BulkUpdateConfigurationsAsync(IEnumerable<ConfigurationUpdate> updates)
        {
            if (updates == null || !updates.Any())
            {
                return Result<BulkUpdateResult>.Failure("Güncellenecek konfigürasyon listesi boş olamaz");
            }

            if (!_environmentService.IsTestEnvironment())
            {
                _logger.LogWarning("Test ortamı dışında toplu konfigürasyon güncelleme denemesi");
                return Result<BulkUpdateResult>.Failure("Konfigürasyon güncellemeleri sadece test ortamında yapılabilir");
            }

            var successfulUpdates = new List<ConfigurationItem>();
            var failedUpdates = new List<FailedUpdate>();

            try
            {
                await _semaphore.WaitAsync();

                // Tüm güncellemeleri tek bir dosya işlemi içinde yapalım
                if (!File.Exists(_settingsFilePath))
                {
                    return Result<BulkUpdateResult>.Failure("Test settings dosyası bulunamadı");
                }

                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var jsonDict = ConvertJsonElementToDictionary(jsonDoc.RootElement);

                foreach (var update in updates)
                {
                    try
                    {
                        // Validasyonlar
                        if (string.IsNullOrEmpty(update.Path))
                        {
                            failedUpdates.Add(new FailedUpdate 
                            { 
                                Update = update, 
                                ErrorMessage = "Konfigürasyon yolu boş olamaz" 
                            });
                            continue;
                        }

                        if (IsRestrictedPath(update.Path))
                        {
                            failedUpdates.Add(new FailedUpdate 
                            { 
                                Update = update, 
                                ErrorMessage = $"'{update.Path}' yolu güncellenemez" 
                            });
                            continue;
                        }

                        // Güncelleme
                        UpdateNestedValue(jsonDict, update.Path.Split(':'), update.Value);

                        // Başarılı güncelleme
                        successfulUpdates.Add(new ConfigurationItem
                        {
                            Path = update.Path,
                            Value = update.Value,
                            Key = update.Path.Split(':').Last()
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Konfigürasyon güncelleme hatası. Path: {Path}, Value: {Value}", 
                            update.Path, update.Value);
                        failedUpdates.Add(new FailedUpdate 
                        { 
                            Update = update, 
                            ErrorMessage = $"Güncelleme hatası: {ex.Message}" 
                        });
                    }
                }

                // Tüm değişiklikleri kaydet
                if (successfulUpdates.Any())
                {
                    var newJson = JsonSerializer.Serialize(
                        jsonDict,
                        new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                    await File.WriteAllTextAsync(_settingsFilePath, newJson);
                    _configRoot.Reload();

                    // Başarılı güncellemeleri logla
                    foreach (var update in successfulUpdates)
                    {
                        await LogConfigurationChangeAsync(update.Path, update.Value);
                    }
                }

                var result = new BulkUpdateResult
                {
                    SuccessfulUpdates = successfulUpdates,
                    FailedUpdates = failedUpdates
                };

                return Result<BulkUpdateResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu güncelleme işlemi sırasında hata oluştu");
                return Result<BulkUpdateResult>.Failure($"Toplu güncelleme hatası: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
