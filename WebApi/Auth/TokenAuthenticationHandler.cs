using System.Security.Claims;
using System.Text.Encodings.Web;
using Hospital.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Hospital.WebApi.Auth;

public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISessionService _sessionService;
    private readonly IUserRepository _userRepository;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock, // TODO: Replace with IDateTimeProvider?
        ISessionService sessionService,
        IUserRepository userRepository)
        : base(options, logger, encoder, clock)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Gets token from headers: Authorization: Bearer {token} or X-Auth-Token
        string? tokenValue = null;
        if (Request.Headers.TryGetValue( "Authorization", out var auth) && auth.Count > 0)
        {
            var authHeader = auth.ToString();
            if (authHeader.StartsWith( "Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenValue = authHeader.Substring( "Bearer ".Length).Trim();
        }

        if (string.IsNullOrWhiteSpace(tokenValue) && Request.Headers.TryGetValue( "X-Auth-Token", out var custom) && custom.Count > 0)
        {
            tokenValue = custom.ToString().Trim();
        }

        if (string.IsNullOrWhiteSpace(tokenValue))
            return AuthenticateResult.NoResult();

        if (!Guid.TryParse(tokenValue, out var tokenGuid))
        {
            // Delay when entering an incorrect (invalid) token to prevent brute-force attacks
            await Task.Delay(TimeSpan.FromSeconds(3), Context.RequestAborted).ConfigureAwait(false);
            return AuthenticateResult.Fail( "Invalid token format." );
        }

        var userId = await _sessionService.ValidateSessionAsync(tokenGuid).ConfigureAwait(false);
        if (!userId.HasValue)
        {
            // Delay when entering an invalid or expired token to prevent brute-force attacks
            await Task.Delay(TimeSpan.FromSeconds(3), Context.RequestAborted).ConfigureAwait(false);
            return AuthenticateResult.Fail( "Invalid or expired token." );
        }

        var user = await _userRepository.GetByIdAsync(userId.Value).ConfigureAwait(false);

        if (user is null)
            return AuthenticateResult.Fail( "User not found." ); // Should never happen.
        if (user.IsBlocked)
            return AuthenticateResult.Fail( "User is blocked." );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Context.Items["CurrentUser"] = user;
        Context.Items["CurrentUserId"] = user.Id;

        return AuthenticateResult.Success(ticket);
    }
}