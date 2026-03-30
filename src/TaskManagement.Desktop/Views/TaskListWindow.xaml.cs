using System.Windows;
using TaskManagement.Desktop.Services;
using TaskManagement.Desktop.ViewModels;

namespace TaskManagement.Desktop.Views;

public partial class TaskListWindow : Window
{
    private readonly TaskListViewModel _viewModel;

    public TaskListWindow(ApiService apiService)
    {
        InitializeComponent();
        _viewModel = new TaskListViewModel(apiService);
        _viewModel.LogoutRequested += OnLogoutRequested;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadTasksAsync();
    }

    private void OnLogoutRequested()
    {
        var apiService = (ApiService)((TaskListViewModel)DataContext!)
            .GetType()
            .GetField("_apiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(DataContext)!;

        new LoginWindow(apiService).Show();
        Close();
    }
}
