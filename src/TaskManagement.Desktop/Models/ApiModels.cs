using Newtonsoft.Json;

namespace TaskManagement.Desktop.Models;

public class LoginRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;

    [JsonProperty("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("user")]
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("fullName")]
    public string FullName => $"{FirstName} {LastName}";

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("tenantId")]
    public int TenantId { get; set; }

    [JsonProperty("tenantName")]
    public string TenantName { get; set; } = string.Empty;
}

public class TaskModel
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonProperty("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonProperty("priorityLabel")]
    public string PriorityLabel { get; set; } = string.Empty;

    [JsonProperty("assignedToName")]
    public string? AssignedToName { get; set; }

    [JsonProperty("createdByName")]
    public string CreatedByName { get; set; } = string.Empty;

    [JsonProperty("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    public bool IsCompleted => Status == "Completed";

    public string DueDateDisplay => DueDate.HasValue
        ? DueDate.Value.ToLocalTime().ToString("MMM d, yyyy")
        : "—";
}

public class TaskSummary
{
    [JsonProperty("totalTasks")]
    public int TotalTasks { get; set; }

    [JsonProperty("todoCount")]
    public int TodoCount { get; set; }

    [JsonProperty("inProgressCount")]
    public int InProgressCount { get; set; }

    [JsonProperty("completedCount")]
    public int CompletedCount { get; set; }
}

public class ApiError
{
    [JsonProperty("error")]
    public string Error { get; set; } = string.Empty;
}
