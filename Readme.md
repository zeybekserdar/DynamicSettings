# Dynamic Settings API

For turkish version of this document, please see [turkish documentation](READMETR.md).

This API provides a service that allows you to dynamically manage your application configurations. You can view and update configurations of your application running in the test environment.

## Features

- 🔍 View all configurations in a hierarchical structure
- 🎯 View configuration at a specific path
- ✏️ Single configuration update
- 📦 Bulk configuration update
- 🔒 Hide configurations containing sensitive information
- 🚫 Prevent updates to critical configurations
- 📝 Log configuration changes
- 🔄 Thread-safe operations

## API Endpoints

### Get All Configurations

```http
GET /api/configuration
```

Returns all configuration values in a hierarchical tree structure.

### Get Specific Configuration

```http
GET /api/configuration/{path}
```

Example:
```http
GET /api/configuration/Logging:LogLevel:Default
```

### Update Configuration

```http
PUT /api/configuration/{path}
Content-Type: application/json

"new_value"
```

Example:
```http
PUT /api/configuration/Logging:LogLevel:Default
Content-Type: application/json

"Information"
```

### Bulk Update

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

## Security Features

### Hidden Configurations

The following paths are hidden from view for security reasons:

- Secrets
- ApiKeys
- Credentials
- PrivateKeys
- Tokens
- ConnectionStrings

### Restricted Configurations

The following paths cannot be updated:

- ConnectionStrings
- Authentication
- Security

## Installation

1. Clone the project
```bash
git clone [repo-url]
```

2. Install dependencies
```bash
dotnet restore
```

3. Run the application
```bash
dotnet run
```

## Usage Example

```csharp
// Configuration service injection
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
            // Update successful
            var updatedConfig = result.Data;
        }
        else
        {
            // Error state
            var errorMessage = result.Error;
        }
    }
}
```

## Error Handling

The API returns results in `Result<T>` type:

```json
{
  "isSuccess": true,
  "data": {
    "path": "Logging:LogLevel:Default",
    "value": "Information",
    "key": "Default"
  },
  "error": null
}
```

In case of error:
```json
{
  "isSuccess": false,
  "data": null,
  "error": "Configuration path not found"
}
```

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: amazing new feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 
