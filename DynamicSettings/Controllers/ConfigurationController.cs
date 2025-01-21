using DynamicSettings.Models;
using DynamicSettings.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DynamicSettings.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationService configurationService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm konfigürasyon değerlerini getirir
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<ConfigurationTree>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConfigurations()
        {
            var result = await _configurationService.GetConfigurationsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Belirtilen yoldaki konfigürasyon değerini getirir
        /// </summary>
        /// <param name="path">Konfigürasyon yolu (örn: "Logging:LogLevel:Default")</param>
        [HttpGet("{*path}")]
        [ProducesResponseType(typeof(Result<ConfigurationItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConfigurationByPath(string path)
        {
            var result = await _configurationService.GetConfigurationByPathAsync(path);
            return Ok(result);
        }

        /// <summary>
        /// Belirtilen yoldaki konfigürasyon değerini günceller
        /// </summary>
        /// <param name="path">Konfigürasyon yolu</param>
        /// <param name="value">Yeni değer</param>
        [HttpPut("{*path}")]
        [ProducesResponseType(typeof(Result<ConfigurationItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfiguration(string path, [FromBody] string value)
        {
            var result = await _configurationService.UpdateConfigurationAsync(path, value);
            return Ok(result);
        }

        /// <summary>
        /// Birden fazla konfigürasyon değerini tek seferde günceller
        /// </summary>
        /// <param name="updates">Güncellenecek konfigürasyon listesi</param>
        [HttpPut("bulk")]
        [ProducesResponseType(typeof(Result<BulkUpdateResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BulkUpdateConfigurations([FromBody] IEnumerable<ConfigurationUpdate> updates)
        {
            var result = await _configurationService.BulkUpdateConfigurationsAsync(updates);
            return Ok(result);
        }
    }
}
