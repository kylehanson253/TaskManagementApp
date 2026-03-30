using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Tenant management — only super-admins (system-level) would typically use this.
/// For demo purposes, it is open to any authenticated Admin to illustrate multi-tenancy.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TenantsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TenantsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetTenants()
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync();
        var dtos = tenants.Select(t => new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            CreatedAt = t.CreatedAt,
            IsActive = t.IsActive,
            UserCount = t.Users.Count,
            TaskCount = t.Tasks.Count
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _unitOfWork.Tenants.GetBySlugAsync(dto.Slug);
        if (existing != null)
            return Conflict(new { error = $"Tenant slug '{dto.Slug}' already exists." });

        var tenant = new Core.Entities.Tenant
        {
            Name = dto.Name,
            Slug = dto.Slug.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Tenants.AddAsync(tenant);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenants), new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            CreatedAt = tenant.CreatedAt,
            IsActive = tenant.IsActive
        });
    }
}
