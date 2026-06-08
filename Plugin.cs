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

        services.AddSingleton(provider =>
        {
            var runtimeContext = provider.GetRequiredService<IAirAppRuntimeContext>();
            Directory.CreateDirectory(runtimeContext.DataDirectory);
            return new ClassworksSettingsService(runtimeContext.DataDirectory);
        });

        services.AddSingleton<ClassworksService>();

        services.AddTransient<ClassworksSettingsViewModel>();

        // Register settings page
        services.AddTransient(provider =>
        {
            var settingsService = provider.GetRequiredService<ClassworksSettingsService>();
            var viewModel = new ClassworksSettingsViewModel(settingsService);
            return new ClassworksSettingsPage(viewModel);
        });

        services.AddAirAppComponent<ClassworksHomeworkWidget>(
            "classworks-homework",
            "Classworks 作业",
            options =>
            {
                options.Description = "在阑山桌面上显示并同步 Classworks 班级作业板数据";
                options.DefaultWidth = 3;
                options.DefaultHeight = 4;
                options.ResizeMode = AirAppComponentResizeMode.Both;
                options.Category = "学习";
                options.IconKey = "Book";
            });
    }

    public override Task OnStartedAsync(IAirAppRuntimeContext context)
    {
        context.Logger.Info("Classworks AirApp started successfully!");
        return Task.CompletedTask;
    }

    public override Task OnStoppingAsync()
    {
        return Task.CompletedTask;
    }
}
