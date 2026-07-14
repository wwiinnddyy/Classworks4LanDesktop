using ClassworksPlugin.Services;
using Xunit;

namespace ClassworksPlugin.Tests;

public sealed class AuthenticationStateTests
{
    [Fact]
    public void ChangingCredentialsClearsCachedTokenBeforeReload()
    {
        var dataDirectory = Path.Combine(
            Path.GetTempPath(),
            "Classworks4LanDesktop.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);

        try
        {
            var settingsService = new ClassworksSettingsService(dataDirectory);
            settingsService.UpdateSettings(
                settings => settings.AppToken = "old-app-token",
                nameof(ClassworksSettings.AppToken));

            using var classworksService = new ClassworksService
            {
                Token = "cached-old-token"
            };
            _ = new ClassworksHomeworkViewModel(settingsService, classworksService);

            settingsService.UpdateSettings(
                settings =>
                {
                    settings.AppToken = string.Empty;
                    settings.NamespaceId = string.Empty;
                    settings.Password = string.Empty;
                },
                nameof(ClassworksSettings.AppToken),
                nameof(ClassworksSettings.NamespaceId),
                nameof(ClassworksSettings.Password));

            Assert.Equal(string.Empty, classworksService.Token);
        }
        finally
        {
            Directory.Delete(dataDirectory, recursive: true);
        }
    }
}
