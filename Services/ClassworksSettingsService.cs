using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassworksPlugin.Services;

public sealed class ClassworksSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [JsonPropertyName("namespaceId")]
    public string NamespaceId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("kvBaseUrl")]
    public string KvBaseUrl { get; set; } = ClassworksService.DefaultKvBaseUrl;

    public static ClassworksSettings Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return new ClassworksSettings();
        }

        try
        {
            var json = File.ReadAllText(filePath).TrimStart('\uFEFF');
            return JsonSerializer.Deserialize<ClassworksSettings>(json, JsonOptions) ?? new ClassworksSettings();
        }
        catch
        {
            return new ClassworksSettings();
        }
    }

    public void Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
        }
    }
}

public sealed class ClassworksSettingsService
{
    private readonly string _settingsPath;
    private ClassworksSettings _settings;
    private readonly object _lock = new();

    public event EventHandler<ClassworksSettings>? SettingsChanged;

    public ClassworksSettingsService(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _settingsPath = Path.Combine(dataDirectory, "classworks.settings.json");
        _settings = ClassworksSettings.Load(_settingsPath);
    }

    public ClassworksSettings GetSettings()
    {
        lock (_lock)
        {
            return _settings;
        }
    }

    public void UpdateSettings(Action<ClassworksSettings> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        lock (_lock)
        {
            updateAction(_settings);
            _settings.Save(_settingsPath);
        }

        SettingsChanged?.Invoke(this, _settings);
    }
}
