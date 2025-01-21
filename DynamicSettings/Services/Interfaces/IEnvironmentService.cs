namespace DynamicSettings.Services.Interfaces
{
    public interface IEnvironmentService
    {
        bool IsTestEnvironment();
        string GetTestSettingsPath();
    }
} 