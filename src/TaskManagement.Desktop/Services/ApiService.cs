using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using TaskManagement.Desktop.Models;

namespace TaskManagement.Desktop.Services;

public class ApiService
{
    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5000/api/")
    };

    private string? _accessToken;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    public UserInfo? CurrentUser { get; private set; }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var payload = new LoginRequest { Email = email, Password = password };
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("auth/login", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<ApiError>(responseBody);
            throw new InvalidOperationException(error?.Error ?? "Login failed.");
        }

        var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseBody)!;
        _accessToken = authResponse.Token;
        CurrentUser = authResponse.User;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        return authResponse;
    }

    public void Logout()
    {
        _accessToken = null;
        CurrentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<List<TaskModel>> GetTasksAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("tasks");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<TaskModel>>(json) ?? new List<TaskModel>();
    }

    public async Task<TaskSummary?> GetTaskSummaryAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("tasks/summary");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TaskSummary>(json);
    }

    public async Task<TaskModel> CompleteTaskAsync(int taskId)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PatchAsync($"tasks/{taskId}/complete", null);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<ApiError>(json);
            throw new InvalidOperationException(error?.Error ?? "Failed to complete task.");
        }

        return JsonConvert.DeserializeObject<TaskModel>(json)!;
    }

    private void EnsureAuthenticated()
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated.");
    }
}
