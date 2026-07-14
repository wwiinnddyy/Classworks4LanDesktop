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

        services.AddSingleton<ClassworksSettingsService>();

        services.AddSingleton<ClassworksService>();

        services.AddTransient<ClassworksSettingsViewModel>();

        services.AddPluginSettingsSection<ClassworksSettingsPage>(
            id: ClassworksSettingsService.SectionId,
            titleLocalizationKey: "settings.page_title",
            descriptionLocalizationKey: "plugin.description",
            iconKey: "Book",
            sortOrder: 0);

        services.AddPluginDesktopComponent<ClassworksHomeworkWidget>(new PluginDesktopComponentOptions
        {
            ComponentId = "Classworks4LanDesktop.Homework",
            DisplayName = "Classworks 作业",
            DisplayNameLocalizationKey = "widget.display_name",
            IconKey = "Book",
            Category = "Classworks",
            MinWidthCells = 3,
            MinHeightCells = 4,
            AllowDesktopPlacement = true,
            AllowStatusBarPlacement = false,
            ResizeMode = PluginDesktopComponentResizeMode.Free,
            CornerRadiusPreset = PluginCornerRadiusPreset.Component
        });
    }
}
