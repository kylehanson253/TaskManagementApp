using System.Windows;
using TaskManagement.Desktop.Services;
using TaskManagement.Desktop.Views;

namespace TaskManagement.Desktop;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var apiService = new ApiService();
        var loginWindow = new LoginWindow(apiService);
        loginWindow.Show();
    }
}
