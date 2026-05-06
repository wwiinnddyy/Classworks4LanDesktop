using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using LanMountainDesktop.PluginSdk;

namespace ClassworksPlugin
{
    /// <summary>
    /// View model for the Classworks widget.  This class exposes a
    /// collection of assignments that the view binds to.  It also handles
    /// loading data from the Classworks service and updating status
    /// indicators for the user.  The view model implements
    /// <see cref="INotifyPropertyChanged"/> so that the UI updates
    /// automatically when properties change.
    /// </summary>
    public sealed class ClassworksViewModel : INotifyPropertyChanged
    {
        private readonly ClassworksService _service = new ClassworksService();
        private bool _isLoading;
        private string _status = string.Empty;

        // The following fields should be bound to the plugin settings page.  They
        // store the namespace, password and appId required to obtain a
        // token from the Classworks API【250439475987970†L32-L68】.
        private string _namespaceId = string.Empty;
        private string _password = string.Empty;
        private string _appId = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassworksViewModel"/> class.
        /// </summary>
        public ClassworksViewModel()
        {
            Assignments = new ObservableCollection<Assignment>();

            // Load persisted settings from disk
            var cfg = ClassworksPlugin.Settings.PluginConfig.Load();
            _namespaceId = cfg.NamespaceId;
            _password = cfg.Password;
            _appId = cfg.AppId;
        }

        // Bound to the input fields for adding a new assignment
        private string _newTitle = string.Empty;
        private string _newDescription = string.Empty;

        /// <summary>
        /// Gets or sets the title of the new assignment to add.
        /// </summary>
        public string NewTitle
        {
            get => _newTitle;
            set
            {
                if (_newTitle != value)
                {
                    _newTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the new assignment to add.
        /// </summary>
        public string NewDescription
        {
            get => _newDescription;
            set
            {
                if (_newDescription != value)
                {
                    _newDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets命名空间，用于向 Classworks 认证服务请求令牌【250439475987970†L42-L50】。
        /// </summary>
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

        /// <summary>
        /// Gets or sets授权码/密码，用于向 Classworks 认证服务请求令牌【250439475987970†L42-L66】。
        /// </summary>
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

        /// <summary>
        /// Gets or sets应用 ID，是在 ZeroCat 社区创建应用后获得的标识【250439475987970†L32-L38】。
        /// </summary>
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
        /// Gets the collection of assignments to display.  Bind your ItemsControl
        /// (e.g. ListBox or ListView) to this property.
        /// </summary>
        public ObservableCollection<Assignment> Assignments { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the assignments are currently
        /// being loaded.  The view can show a progress indicator when this is
        /// true.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a status message displayed to the user.  This may
        /// contain error messages, login prompts or other information.
        /// </summary>
        public string Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Attempts to authenticate with the Classworks service and load the
        /// current assignments.  If the user has not configured their
        /// credentials a login prompt will be displayed.  Errors are captured
        /// and reported via the Status property.
        /// </summary>
        public async Task LoadAssignmentsAsync()
        {
            try
            {
                IsLoading = true;
                Status = "正在加载作业...";
                // If no token exists, request a new one using the configured settings.
                if (string.IsNullOrEmpty(_service.Token))
                {
                    if (string.IsNullOrEmpty(NamespaceId) ||
                        string.IsNullOrEmpty(Password) ||
                        string.IsNullOrEmpty(AppId))
                    {
                        Status = "请在设置中填写命名空间、密码和应用 ID。";
                        return;
                    }
                    var token = await _service.AuthenticateAsync(NamespaceId, Password, AppId);
                    if (string.IsNullOrEmpty(token))
                    {
                        Status = "登录失败，请检查配置。";
                        return;
                    }
                    _service.Token = token;
                }

                var assignments = await _service.GetAssignmentsAsync(DateTime.Today);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Assignments.Clear();
                    foreach (var assignment in assignments)
                    {
                        Assignments.Add(assignment);
                    }
                    Status = assignments.Count == 0 ? "暂无作业" : string.Empty;
                });
            }
            catch (Exception ex)
            {
                Status = $"加载作业失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Adds a new assignment using the values from <see cref="NewTitle"/> and
        /// <see cref="NewDescription"/>.  After adding locally, this method
        /// synchronises the updated assignments to the Classworks KV store.
        /// </summary>
        public async Task AddAssignmentAsync()
        {
            // Validate input
            var title = NewTitle?.Trim();
            var desc = NewDescription?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                Status = "请输入科目名称";
                return;
            }

            // Add assignment locally
            var assignment = new Assignment
            {
                Title = title,
                Description = desc ?? string.Empty,
                DueDate = DateTime.Today
            };
            Assignments.Add(assignment);
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            Status = string.Empty;

            try
            {
                // Ensure token
                if (string.IsNullOrEmpty(_service.Token))
                {
                    if (string.IsNullOrEmpty(NamespaceId) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(AppId))
                    {
                        Status = "请在设置中填写命名空间、密码和应用 ID。";
                        return;
                    }
                    var token = await _service.AuthenticateAsync(NamespaceId, Password, AppId);
                    if (string.IsNullOrEmpty(token))
                    {
                        Status = "登录失败，请检查配置。";
                        return;
                    }
                    _service.Token = token;
                }
                // Persist assignments to remote KV store
                await _service.AddAssignmentAsync(assignment, DateTime.Today, Assignments);
            }
            catch (Exception ex)
            {
                Status = $"同步作业失败: {ex.Message}";
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}