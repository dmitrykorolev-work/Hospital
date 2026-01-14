using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Enums;
using Hospital.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.WebApi.Controllers;

[Route( "api/[controller]" )]
[Authorize(Roles = "Superadmin" )]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public AdminController(IUserService userService, IAuditService auditService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // POST: api/Admin/
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminRegisterDto dto)
    {
        if (dto is null) return BadRequest();

        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ( string.IsNullOrWhiteSpace( dto.Email ) ) return BadRequest("Email is required.");
            if ( string.IsNullOrWhiteSpace( dto.Password ) ) return BadRequest("Password is required.");

            if (await _userService.GetByEmailAsync(dto.Email).ConfigureAwait(false) is not null)
            {
                return BadRequest("Email is already registered.");
            }

            var createUserDto = new CreateUserDto(dto.Email, dto.Password, Role.Admin);
            var createdUser = await _userService.CreateAsync(createUserDto).ConfigureAwait(false);

            var userId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(userId, AuditAct.User, $"Created admin. Email: {dto?.Email}");

            return Ok();
        }
        catch (Exception ex)
        {
            var userId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(userId, AuditAct.User, $"Create admin failed: {ex.Message} Email: {dto?.Email}");
            return Problem(detail: $"An error occurred: {ex.Message}", statusCode: 500);
        }
    }

    private async Task SafeLogAsync(Guid? userId, AuditAct action, string? details = null)
    {
        try
        {
            await _auditService.LogAsync(userId, action, details).ConfigureAwait(false);
        }
        catch
        {
            // Logging should not break API
        }
    }
}