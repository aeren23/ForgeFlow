using ForgeFlow.Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Identity.Application.Auth.Commands;

/// <summary>
/// Kullanıcı kayıt handler'ı - ASP.NET Core Identity UserManager kullanır
/// </summary>
public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(UserManager<ApplicationUser> userManager, ILogger<RegisterHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Email unique kontrolü
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            return new RegisterResult(false, null, null, new[] { "Bu email adresi zaten kullanılıyor." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User registered successfully: {Email} ({UserId})", user.Email, user.Id);
            return new RegisterResult(true, user.Id, user.Email, null);
        }

        var errors = result.Errors.Select(e => e.Description);
        _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));
        return new RegisterResult(false, null, null, errors);
    }
}
