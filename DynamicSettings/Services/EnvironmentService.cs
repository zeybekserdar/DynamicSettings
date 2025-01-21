using DynamicSettings.Constants;
using DynamicSettings.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace DynamicSettings.Services
{
    public class EnvironmentService : IEnvironmentService
    {
        private readonly IHostEnvironment _hostEnvironment;

        public EnvironmentService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public bool IsTestEnvironment()
        {
            return _hostEnvironment.IsDevelopment();
        }

        public string GetTestSettingsPath()
        {
            return Path.Combine(_hostEnvironment.ContentRootPath, "appsettings.Development.json");
        }
    }
} 