using System.Windows;
using TaskManagement.Desktop.Services;
using TaskManagement.Desktop.ViewModels;

namespace TaskManagement.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(ApiService apiService)
    {
        InitializeComponent();
        _viewModel = new LoginViewModel(apiService);
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        DataContext = _viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
    }

    private void OnLoginSucceeded()
    {
        var taskListWindow = new TaskListWindow((ApiService)((LoginViewModel)DataContext!)
            .GetType()
            .GetField("_apiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(DataContext)!);

        taskListWindow.Show();
        Close();
    }
}
