using ClassworksPlugin.Services;
using ClassworksPlugin.ViewModels.Settings;
using ClassworksPlugin.Views.Settings;
using ClassworksPlugin.Widgets;
using LanMountainDesktop.AirAppSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassworksPlugin;

[AirAppEntrance]
public sealed class Plugin : AirAppBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ClassworksSettingsService>();

        services.AddSingleton<ClassworksService>();

        services.AddTransient<ClassworksSettingsViewModel>();

        services.AddAirAppSettingsSection<ClassworksSettingsPage>(
            id: ClassworksSettingsService.SectionId,
            titleLocalizationKey: "settings.page_title",
            descriptionLocalizationKey: "plugin.description",
            iconKey: "Book",
            sortOrder: 0);

        services.AddAirAppComponent<ClassworksHomeworkWidget>(new AirAppComponentOptions
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
            ResizeMode = AirAppComponentResizeMode.Free,
            CornerRadiusPreset = AirAppCornerRadiusPreset.Component
        });
    }
}
