using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.WebApi.Controllers;

[Route( "api/[controller]" )]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public AuthController(IAuthService authService, IAuditService auditService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // POST: api/Users/register
    [HttpPost( "register" )]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] UserRegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(dto, Role.Patient).ConfigureAwait(false);

        // Logging a registration attempt (Without userId - as system event)
        await SafeLogAsync(null, AuditAct.User, $"Register attempt Email: {dto?.Email} Success: {result.Success}"
              + (result.Message is not null ? $" Message: {result.Message}" : "" ));

        if (!result.Success)
        {
            // Simulate delay to prevent brute-force attacks
            await Task.Delay(TimeSpan.FromSeconds(3), HttpContext.RequestAborted).ConfigureAwait(false);
        }

        return Ok(result);
    }

    // POST: api/Users/login
    [HttpPost( "login" )]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] UserLoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto).ConfigureAwait(false);

        // Logging login attempt (UserId is unknown until successful auth.)
        await SafeLogAsync(null, AuditAct.User, $"Login attempt Email: {dto?.Email} Success: {result.Success}"
              + (result.Message is not null ? $" Message: {result.Message}" : "" ));

        if ( !result.Success )
        {
            // Simulate delay to prevent brute-force attacks
            await Task.Delay(TimeSpan.FromSeconds(3), HttpContext.RequestAborted).ConfigureAwait(false);
        }

        return Ok(result);
    }

    private async Task SafeLogAsync(Guid? userId, AuditAct action, string? details = null)
    {
        try
        {
            await _auditService.LogAsync(userId, action, details).ConfigureAwait(false);
        }
        catch
        {
            // Logging must not break API
        }
    }
}