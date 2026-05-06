using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ClassworksPlugin.Settings;

namespace ClassworksPlugin.ViewModels.Settings
{
    /// <summary>
    /// View model for the plugin settings page.  Allows the user to
    /// configure the Classworks KV connection using either direct input or
    /// browser login.  Changes are persisted via <see cref="PluginConfig"/>.
    /// </summary>
    public sealed class ClassworksSettingsViewModel : INotifyPropertyChanged
    {
        private int _loginMethodIndex;
        private string _namespaceId = string.Empty;
        private string _password = string.Empty;
        private string _appId = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassworksSettingsViewModel"/> class.
        /// Loads existing configuration from disk.
        /// </summary>
        public ClassworksSettingsViewModel()
        {
            var cfg = PluginConfig.Load();
            _namespaceId = cfg.NamespaceId;
            _password = cfg.Password;
            _appId = cfg.AppId;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 选择的登录方式索引：0 表示直接填写，1 表示浏览器登录。
        /// </summary>
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

        /// <summary>
        /// 是否处于直接输入模式。
        /// </summary>
        public bool IsDirectInputMode => LoginMethodIndex == 0;

        /// <summary>
        /// 是否处于浏览器登录模式。
        /// </summary>
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

        /// <summary>
        /// Saves settings to disk.  Widgets will reload configuration on next
        /// refresh.
        /// </summary>
        public Task SaveSettingsAsync()
        {
            var cfg = new PluginConfig
            {
                NamespaceId = NamespaceId ?? string.Empty,
                Password = Password ?? string.Empty,
                AppId = AppId ?? string.Empty
            };
            cfg.Save();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initiates a browser-based login.  This simply opens the default
        /// browser to the ZeroCat OAuth page; the user completes login and
        /// authorises the application.  After completion they should copy
        /// the returned token into the plugin settings.  Real implementation
        /// would involve a local redirect URI and a callback listener.
        /// </summary>
        public Task BeginBrowserLoginAsync()
        {
            try
            {
                // Launch default browser to the OAuth page.  Replace with the
                // correct URL for ZeroCat OAuth once available.
                var loginUrl = "https://zerocat.dev/oauth";
                Process.Start(new ProcessStartInfo(loginUrl) { UseShellExecute = true });
            }
            catch
            {
                // ignore
            }
            return Task.CompletedTask;
        }
    }
}