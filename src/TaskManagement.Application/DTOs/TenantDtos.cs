using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public int TaskCount { get; set; }
}

public class CreateTenantDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50), RegularExpression(@"^[a-z0-9-]+$",
        ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]
    public string Slug { get; set; } = string.Empty;
}
