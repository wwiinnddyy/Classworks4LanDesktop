using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClassworksPlugin.Services;

namespace ClassworksPlugin.ViewModels.Settings;

public sealed class ClassworksSettingsViewModel : INotifyPropertyChanged
{
    private readonly ClassworksSettingsService _settingsService;
    private int _loginMethodIndex;
    private string _namespaceId = string.Empty;
    private string _password = string.Empty;
    private string _appId = string.Empty;
    private string _kvBaseUrl = ClassworksService.DefaultKvBaseUrl;

    public ClassworksSettingsViewModel(ClassworksSettingsService settingsService)
    {
        _settingsService = settingsService;

        var settings = _settingsService.GetSettings();
        _namespaceId = settings.NamespaceId;
        _password = settings.Password;
        _appId = settings.AppId;
        _kvBaseUrl = settings.KvBaseUrl;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int LoginMethodIndex
    {
        get => _loginMethodIndex;
        set
        {
            if (_loginMethodIndex != value)
            {
                _loginMethodIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDirectInputMode));
                OnPropertyChanged(nameof(IsBrowserMode));
            }
        }
    }

    public bool IsDirectInputMode => LoginMethodIndex == 0;

    public bool IsBrowserMode => LoginMethodIndex == 1;

    public string NamespaceId
    {
        get => _namespaceId;
        set
        {
            if (_namespaceId != value)
            {
                _namespaceId = value;
                OnPropertyChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
            }
        }
    }

    public string AppId
    {
        get => _appId;
        set
        {
            if (_appId != value)
            {
                _appId = value;
                OnPropertyChanged();
            }
        }
    }

    public string KvBaseUrl
    {
        get => _kvBaseUrl;
        set
        {
            if (_kvBaseUrl != value)
            {
                _kvBaseUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string PageTitle => "Classworks 设置";

    public Task SaveSettingsAsync()
    {
        _settingsService.UpdateSettings(settings =>
        {
            settings.NamespaceId = NamespaceId ?? string.Empty;
            settings.Password = Password ?? string.Empty;
            settings.AppId = AppId ?? string.Empty;
            settings.KvBaseUrl = string.IsNullOrWhiteSpace(KvBaseUrl)
                ? ClassworksService.DefaultKvBaseUrl
                : KvBaseUrl.Trim();
        });
        return Task.CompletedTask;
    }

    public Task BeginBrowserLoginAsync()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://zerocat.dev/oauth")
            {
                UseShellExecute = true
            });
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
