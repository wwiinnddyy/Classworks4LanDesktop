using ClassworksPlugin.Services;
using ClassworksPlugin.ViewModels.Settings;
using ClassworksPlugin.Views.Settings;
using ClassworksPlugin.Widgets;
using LanMountainDesktop.PluginSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassworksPlugin;

[PluginEntrance]
public sealed class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(services);

        var localizer = CreateLocalizer(context);

        services.AddSingleton(provider =>
        {
            var runtimeContext = provider.GetRequiredService<IPluginRuntimeContext>();
            Directory.CreateDirectory(runtimeContext.DataDirectory);
            return new ClassworksSettingsService(runtimeContext.DataDirectory);
        });

        services.AddSingleton<ClassworksService>();

        services.AddTransient<ClassworksSettingsViewModel>(provider =>
        {
            var settingsService = provider.GetRequiredService<ClassworksSettingsService>();
            return new ClassworksSettingsViewModel(settingsService, localizer);
        });

        services.AddPluginSettingsSection<ClassworksSettingsPage>(
            id: "classworks-settings",
            titleLocalizationKey: "settings.page_title",
            descriptionLocalizationKey: "plugin.description",
            iconKey: "Book",
            sortOrder: 0);

        services.AddPluginDesktopComponent<ClassworksHomeworkWidget>(
            CreateHomeworkComponentOptions(localizer));
    }

    private static PluginLocalizer CreateLocalizer(HostBuilderContext context)
    {
        var pluginDirectory = context.Properties.TryGetValue("LanMountainDesktop.PluginDirectory", out var directoryValue) &&
                              directoryValue is string resolvedPluginDirectory &&
                              !string.IsNullOrWhiteSpace(resolvedPluginDirectory)
            ? resolvedPluginDirectory
            : AppContext.BaseDirectory;

        var properties = context.Properties
            .Where(pair => pair.Key is string)
            .ToDictionary(pair => (string)pair.Key, pair => (object?)pair.Value, StringComparer.OrdinalIgnoreCase);

        return new PluginLocalizer(pluginDirectory, PluginLocalizer.ResolveLanguageCode(properties));
    }

    private static PluginDesktopComponentOptions CreateHomeworkComponentOptions(PluginLocalizer localizer)
    {
        return new PluginDesktopComponentOptions
        {
            ComponentId = "Classworks4LanDesktop.Homework",
            DisplayName = localizer.GetString("widget.display_name", "Classworks 作业"),
            DisplayNameLocalizationKey = "widget.display_name",
            IconKey = "Book",
            Category = localizer.GetString("widget.category", "Classworks"),
            MinWidthCells = 3,
            MinHeightCells = 4,
            AllowDesktopPlacement = true,
            AllowStatusBarPlacement = false,
            ResizeMode = PluginDesktopComponentResizeMode.Proportional,
            CornerRadiusPreset = PluginCornerRadiusPreset.Default
        };
    }
}
