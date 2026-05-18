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
    private string _status = string.Empty;
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
            if (string.IsNullOrWhiteSpace(settings.NamespaceId) ||
                string.IsNullOrWhiteSpace(settings.Password) ||
                string.IsNullOrWhiteSpace(settings.AppId))
            {
                Status = "请在插件设置中填写命名空间、密码和应用 ID。";
                return;
            }

            if (string.IsNullOrEmpty(_service.Token))
            {
                var token = await _service.AuthenticateAsync(
                    settings.NamespaceId,
                    settings.Password,
                    settings.AppId,
                    settings.KvBaseUrl);
                if (string.IsNullOrEmpty(token))
                {
                    Status = "登录失败，请检查设置。";
                    return;
                }

                _service.Token = token;
            }

            var assignments = await _service.GetAssignmentsAsync(DateTime.Today, settings.KvBaseUrl);
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

    public async Task AddAssignmentAsync()
    {
        var title = NewTitle?.Trim();
        var desc = NewDescription?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            Status = "请输入科目名称";
            return;
        }

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
            var settings = _settingsService.GetSettings();
            if (string.IsNullOrEmpty(_service.Token))
            {
                var token = await _service.AuthenticateAsync(
                    settings.NamespaceId,
                    settings.Password,
                    settings.AppId,
                    settings.KvBaseUrl);
                if (string.IsNullOrEmpty(token))
                {
                    Status = "登录失败，请检查设置。";
                    return;
                }

                _service.Token = token;
            }

            await _service.AddAssignmentAsync(assignment, DateTime.Today, Assignments, settings.KvBaseUrl);
        }
        catch (Exception ex)
        {
            Status = $"同步作业失败: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
