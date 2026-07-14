using ClassworksPlugin.Services;
using LanMountainDesktop.PluginSdk;
using Xunit;

namespace ClassworksPlugin.Tests;

public sealed class SettingsMigrationTests
{
    [Fact]
    public void ImportsLegacySettingsIntoHostPluginScopeAndReturnsClones()
    {
        var dataDirectory = Path.Combine(Path.GetTempPath(), "Classworks4LanDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);

        try
        {
            File.WriteAllText(
                Path.Combine(dataDirectory, "classworks.settings.json"),
                """
                {
                  "namespaceId": " legacy-space ",
                  "password": "secret",
                  "appId": "",
                  "appToken": " legacy-token ",
                  "kvBaseUrl": "https://kv-service.houlang.cloud/"
                }
                """);

            var hostSettings = new RecordingSettingsService();
            var runtimeContext = new TestRuntimeContext(dataDirectory);

            var service = new ClassworksSettingsService(hostSettings, runtimeContext);
            var imported = service.GetSettings();

            Assert.Equal("legacy-space", imported.NamespaceId);
            Assert.Equal("secret", imported.Password);
            Assert.Equal(ClassworksService.DefaultAppId, imported.AppId);
            Assert.Equal("legacy-token", imported.AppToken);
            Assert.Equal(ClassworksService.DefaultKvBaseUrl, imported.KvBaseUrl);
            Assert.True(imported.LegacySettingsImported);
            Assert.Equal(SettingsScope.Plugin, hostSettings.LastScope);
            Assert.Equal("Classworks4LanDesktop", hostSettings.LastSubjectId);
            Assert.Equal(ClassworksSettingsService.SectionId, hostSettings.LastSectionId);

            imported.AppToken = "mutated-outside-service";
            Assert.Equal("legacy-token", service.GetSettings().AppToken);

            service.UpdateSettings(
                settings => settings.AppToken = "updated-token",
                nameof(ClassworksSettings.AppToken));

            Assert.Equal("updated-token", hostSettings.SavedSettings?.AppToken);
            Assert.Contains(nameof(ClassworksSettings.AppToken), hostSettings.LastChangedKeys ?? []);
        }
        finally
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private sealed class RecordingSettingsService : ISettingsService
    {
        public event EventHandler<SettingsChangedEvent>? Changed;

        public ClassworksSettings? SavedSettings { get; private set; }

        public SettingsScope? LastScope { get; private set; }

        public string? LastSubjectId { get; private set; }

        public string? LastSectionId { get; private set; }

        public IReadOnlyCollection<string>? LastChangedKeys { get; private set; }

        public T LoadSnapshot<T>(SettingsScope scope, string? subjectId = null, string? placementId = null) where T : new() => new();

        public void SaveSnapshot<T>(SettingsScope scope, T snapshot, string? subjectId = null, string? placementId = null, string? sectionId = null, IReadOnlyCollection<string>? changedKeys = null) =>
            throw new NotSupportedException();

        public T LoadSection<T>(SettingsScope scope, string subjectId, string sectionId, string? placementId = null) where T : new() => new();

        public void SaveSection<T>(SettingsScope scope, string subjectId, string sectionId, T section, string? placementId = null, IReadOnlyCollection<string>? changedKeys = null)
        {
            LastScope = scope;
            LastSubjectId = subjectId;
            LastSectionId = sectionId;
            LastChangedKeys = changedKeys;
            SavedSettings = Assert.IsType<ClassworksSettings>(section).Clone();
            Changed?.Invoke(this, null!);
        }

        public void DeleteSection(SettingsScope scope, string subjectId, string sectionId, string? placementId = null) =>
            throw new NotSupportedException();

        public T? GetValue<T>(SettingsScope scope, string key, string? subjectId = null, string? placementId = null, string? sectionId = null) =>
            default;

        public void SetValue<T>(SettingsScope scope, string key, T value, string? subjectId = null, string? placementId = null, string? sectionId = null, IReadOnlyCollection<string>? changedKeys = null) =>
            throw new NotSupportedException();

        public IComponentSettingsAccessor GetComponentAccessor(string componentId, string? placementId) =>
            throw new NotSupportedException();
    }

    private sealed class TestRuntimeContext(string dataDirectory) : IPluginRuntimeContext
    {
        public PluginManifest Manifest { get; } = new(
            "Classworks4LanDesktop",
            "Classworks Homework",
            "ClassworksPlugin.dll",
            Version: "0.2.0",
            ApiVersion: "5.0.0",
            Runtime: new PluginRuntimeConfiguration { Mode = "in-proc" });

        public string PluginDirectory => dataDirectory;

        public string DataDirectory => dataDirectory;

        public IServiceProvider Services => null!;

        public IReadOnlyDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

        public IPluginAppearanceContext Appearance => null!;

        public T? GetService<T>() => default;

        public bool TryGetProperty<T>(string key, out T? value)
        {
            value = default;
            return false;
        }
    }
}
