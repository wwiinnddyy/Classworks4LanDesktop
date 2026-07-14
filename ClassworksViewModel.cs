using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using ClassworksPlugin.Models;
using ClassworksPlugin.Services;

namespace ClassworksPlugin;

public sealed class ClassworksHomeworkViewModel : INotifyPropertyChanged
{
    private readonly ClassworksSettingsService _settingsService;
    private readonly ClassworksService _service;
    private bool _isLoading;
    private string _status = "准备就绪";
    private string _newTitle = string.Empty;
    private string _newDescription = string.Empty;

    public ClassworksHomeworkViewModel(
        ClassworksSettingsService settingsService,
        ClassworksService service)
    {
        _settingsService = settingsService;
        _service = service;
        Assignments = new ObservableCollection<Assignment>();
        _settingsService.SettingsChanged += (_, _) => _ = LoadAssignmentsAsync();
    }

    public ObservableCollection<Assignment> Assignments { get; }

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

    public async Task LoadAssignmentsAsync()
    {
        try
        {
            IsLoading = true;
            Status = "正在加载作业...";

            var settings = _settingsService.GetSettings();
            if (!HasConnectionCredentials(settings))
            {
                Status = "⚠️ 请先在设置中填写 App Token，或填写命名空间与密码";
                return;
            }

            if (!await EnsureAuthenticatedAsync(settings))
            {
                Status = "❌ 认证失败，请检查 App Token 或命名空间信息";
                return;
            }

            // Load today's assignments
            var assignments = await _service.GetAssignmentsAsync(DateTime.Today, settings.KvBaseUrl);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Assignments.Clear();
                foreach (var assignment in assignments)
                {
                    Assignments.Add(assignment);
                }

                Status = assignments.Count == 0
                    ? "📝 暂无作业，点击下方添加今日作业"
                    : $"✅ 已加载 {assignments.Count} 条作业 - {DateTime.Now:HH:mm}";
            });
        }
        catch (HttpRequestException ex)
        {
            Status = $"🌐 网络错误：{ex.Message}";
        }
        catch (Exception ex)
        {
            Status = $"❌ 加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AddAssignmentAsync()
    {
        var title = NewTitle?.Trim();
        var desc = NewDescription?.Trim();

        if (string.IsNullOrEmpty(title))
        {
            Status = "⚠️ 请输入科目名称";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "正在添加作业...";

            var assignment = new Assignment
            {
                Title = title,
                Description = desc ?? string.Empty,
                DueDate = DateTime.Today.AddHours(23).AddMinutes(59)
            };

            var settings = _settingsService.GetSettings();

            if (!HasConnectionCredentials(settings))
            {
                Status = "⚠️ 请先配置 Classworks App Token 或命名空间信息";
                return;
            }

            if (!await EnsureAuthenticatedAsync(settings))
            {
                Status = "❌ 认证失败，无法添加作业";
                return;
            }

            // Add to local list first
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Assignments.Add(assignment);
            });

            // Sync to cloud
            await _service.SaveAssignmentsAsync(DateTime.Today, Assignments, settings.KvBaseUrl);

            // Clear form
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            Status = $"✅ 作业已添加并同步到云端 - {DateTime.Now:HH:mm}";
        }
        catch (HttpRequestException ex)
        {
            Status = $"🌐 同步失败：{ex.Message}";
            // Remove from local list if sync failed
            if (!string.IsNullOrEmpty(title))
            {
                var toRemove = Assignments.FirstOrDefault(a => a.Title == title);
                if (toRemove != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => Assignments.Remove(toRemove));
                }
            }
        }
        catch (Exception ex)
        {
            Status = $"❌ 添加失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static bool HasConnectionCredentials(ClassworksSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.AppToken) ||
               (!string.IsNullOrWhiteSpace(settings.NamespaceId) &&
                !string.IsNullOrWhiteSpace(settings.Password));
    }

    private async Task<bool> EnsureAuthenticatedAsync(ClassworksSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.AppToken))
        {
            _service.Token = settings.AppToken.Trim();
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_service.Token))
        {
            return true;
        }

        Status = "正在认证...";
        var token = await _service.AuthenticateAsync(
            settings.NamespaceId,
            settings.Password,
            settings.AppId,
            settings.KvBaseUrl);

        return !string.IsNullOrWhiteSpace(token);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
