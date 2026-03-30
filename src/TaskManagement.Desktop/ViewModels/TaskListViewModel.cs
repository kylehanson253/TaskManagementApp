using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskManagement.Desktop.Models;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop.ViewModels;

public class TaskListViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private TaskModel? _selectedTask;
    private TaskSummary? _summary;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LogoutRequested;

    public ObservableCollection<TaskModel> Tasks { get; } = new();

    public TaskModel? SelectedTask
    {
        get => _selectedTask;
        set { _selectedTask = value; OnPropertyChanged(); CompleteTaskCommand.RaiseCanExecuteChanged(); }
    }

    public TaskSummary? Summary
    {
        get => _summary;
        set { _summary = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public string WelcomeMessage =>
        _apiService.CurrentUser is { } u
            ? $"Welcome, {u.FullName}  |  {u.TenantName}"
            : string.Empty;

    public RelayCommand LoadTasksCommand { get; }
    public RelayCommand CompleteTaskCommand { get; }
    public RelayCommand LogoutCommand { get; }

    public TaskListViewModel(ApiService apiService)
    {
        _apiService = apiService;
        LoadTasksCommand = new RelayCommand(_ => LoadTasksAsync());
        CompleteTaskCommand = new RelayCommand(
            _ => CompleteSelectedTaskAsync(),
            _ => SelectedTask != null && !SelectedTask.IsCompleted);
        LogoutCommand = new RelayCommand(_ =>
        {
            _apiService.Logout();
            LogoutRequested?.Invoke();
            return Task.CompletedTask;
        });
    }

    public async Task LoadTasksAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading tasks…";
        Tasks.Clear();

        try
        {
            var tasks = await _apiService.GetTasksAsync();
            foreach (var t in tasks)
                Tasks.Add(t);

            Summary = await _apiService.GetTaskSummaryAsync();
            StatusMessage = $"{Tasks.Count} task(s) loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CompleteSelectedTaskAsync()
    {
        if (SelectedTask == null) return;

        IsLoading = true;
        try
        {
            var updated = await _apiService.CompleteTaskAsync(SelectedTask.Id);
            var idx = Tasks.IndexOf(SelectedTask);
            if (idx >= 0) Tasks[idx] = updated;
            StatusMessage = $"Task '{updated.Title}' marked complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
