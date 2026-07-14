using System.Text.Json;
using System.Text.Json.Serialization;
using LanMountainDesktop.PluginSdk;

namespace ClassworksPlugin.Services;

public sealed class ClassworksSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [JsonPropertyName("namespaceId")]
    public string NamespaceId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = ClassworksService.DefaultAppId;

    [JsonPropertyName("appToken")]
    public string AppToken { get; set; } = string.Empty;

    [JsonPropertyName("kvBaseUrl")]
    public string KvBaseUrl { get; set; } = ClassworksService.DefaultKvBaseUrl;

    [JsonPropertyName("legacySettingsImported")]
    public bool LegacySettingsImported { get; set; }

    public ClassworksSettings Clone() => new()
    {
        NamespaceId = NamespaceId,
        Password = Password,
        AppId = AppId,
        AppToken = AppToken,
        KvBaseUrl = KvBaseUrl,
        LegacySettingsImported = LegacySettingsImported
    };

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
            var settings = JsonSerializer.Deserialize<ClassworksSettings>(json, JsonOptions) ?? new ClassworksSettings();
            if (string.IsNullOrWhiteSpace(settings.AppId))
            {
                settings.AppId = ClassworksService.DefaultAppId;
            }

            if (string.IsNullOrWhiteSpace(settings.KvBaseUrl))
            {
                settings.KvBaseUrl = ClassworksService.DefaultKvBaseUrl;
            }

            return settings;
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
    public const string SectionId = "connection";

    private readonly ISettingsService? _hostSettingsService;
    private readonly IPluginRuntimeContext? _runtimeContext;
    private readonly string? _settingsPath;
    private ClassworksSettings _settings;
    private readonly object _syncRoot = new();

    public event EventHandler<ClassworksSettings>? SettingsChanged;

    public ClassworksSettingsService(
        ISettingsService settingsService,
        IPluginRuntimeContext runtimeContext)
    {
        _hostSettingsService = settingsService;
        _runtimeContext = runtimeContext;
        Directory.CreateDirectory(runtimeContext.DataDirectory);

        _settings = settingsService.LoadSection<ClassworksSettings>(
            SettingsScope.Plugin,
            runtimeContext.Manifest.Id,
            SectionId);
        Normalize(_settings);
        ImportLegacySettingsIfRequired();
    }

    // Used by the parameterless design-time widget constructor only.
    public ClassworksSettingsService(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _settingsPath = Path.Combine(dataDirectory, "classworks.settings.json");
        _settings = ClassworksSettings.Load(_settingsPath);
        Normalize(_settings);
    }

    public ClassworksSettings GetSettings()
    {
        lock (_syncRoot)
        {
            return _settings.Clone();
        }
    }

    public void UpdateSettings(
        Action<ClassworksSettings> updateAction,
        params string[] changedKeys)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        ClassworksSettings snapshot;
        lock (_syncRoot)
        {
            updateAction(_settings);
            Normalize(_settings);
            snapshot = _settings.Clone();
        }

        Save(snapshot, changedKeys);
        SettingsChanged?.Invoke(this, snapshot.Clone());
    }

    private void ImportLegacySettingsIfRequired()
    {
        if (_runtimeContext is null || _settings.LegacySettingsImported)
        {
            return;
        }

        var legacyPath = Path.Combine(_runtimeContext.DataDirectory, "classworks.settings.json");
        if (File.Exists(legacyPath))
        {
            var legacy = ClassworksSettings.Load(legacyPath);
            _settings.NamespaceId = legacy.NamespaceId;
            _settings.Password = legacy.Password;
            _settings.AppId = legacy.AppId;
            _settings.AppToken = legacy.AppToken;
            _settings.KvBaseUrl = legacy.KvBaseUrl;
        }

        _settings.LegacySettingsImported = true;
        Normalize(_settings);
        Save(_settings.Clone(),
        [
            nameof(ClassworksSettings.NamespaceId),
            nameof(ClassworksSettings.Password),
            nameof(ClassworksSettings.AppId),
            nameof(ClassworksSettings.AppToken),
            nameof(ClassworksSettings.KvBaseUrl),
            nameof(ClassworksSettings.LegacySettingsImported)
        ]);
    }

    private void Save(ClassworksSettings snapshot, IReadOnlyCollection<string>? changedKeys)
    {
        if (_hostSettingsService is not null && _runtimeContext is not null)
        {
            _hostSettingsService.SaveSection(
                SettingsScope.Plugin,
                _runtimeContext.Manifest.Id,
                SectionId,
                snapshot,
                changedKeys: changedKeys is { Count: > 0 } ? changedKeys : null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_settingsPath))
        {
            snapshot.Save(_settingsPath);
        }
    }

    private static void Normalize(ClassworksSettings settings)
    {
        settings.NamespaceId = settings.NamespaceId?.Trim() ?? string.Empty;
        settings.Password ??= string.Empty;
        settings.AppToken = settings.AppToken?.Trim() ?? string.Empty;
        settings.AppId = string.IsNullOrWhiteSpace(settings.AppId)
            ? ClassworksService.DefaultAppId
            : settings.AppId.Trim();
        settings.KvBaseUrl = string.IsNullOrWhiteSpace(settings.KvBaseUrl)
            ? ClassworksService.DefaultKvBaseUrl
            : settings.KvBaseUrl.Trim().TrimEnd('/');
    }
}
