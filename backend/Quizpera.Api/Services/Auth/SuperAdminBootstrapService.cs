using Microsoft.AspNetCore.Identity;
using Quizpera.Api.Domain.Auth;

namespace Quizpera.Api.Services.Auth;

public sealed class SuperAdminBootstrapService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SuperAdminBootstrapService> _logger;

    public SuperAdminBootstrapService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<SuperAdminBootstrapService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "SuperAdmin bootstrap is only permitted in Development.");
        }

        if (!_configuration.GetValue<bool>(
                "SuperAdminBootstrap:Enabled"))
        {
            return;
        }

        var email = _configuration[
            "SuperAdminBootstrap:Email"]?.Trim();

        var password = _configuration[
            "SuperAdminBootstrap:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SuperAdmin bootstrap requires an email and password.");
        }

        if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
        {
            throw new InvalidOperationException(
                "The SuperAdmin role does not exist. Run role seeding first.");
        }

        var existingSuperAdmins =
            await _userManager.GetUsersInRoleAsync("SuperAdmin");

        if (existingSuperAdmins.Count > 0)
        {
            throw new InvalidOperationException(
                "A SuperAdmin already exists. Bootstrap refused.");
        }

        // Never promote an existing account through bootstrap.
        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "A SuperAdmin bootstrap account already exists. " +
                "Refusing to promote an existing account.");
        }

        // Bootstrap may create a new account only.
        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false
        };

        var accountCreationResult =
            await _userManager.CreateAsync(newUser, password);

        if (!accountCreationResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                accountCreationResult.Errors.Select(error =>
                    $"{error.Code}: {error.Description}"));

            throw new InvalidOperationException(
                $"SuperAdmin account creation failed: {errors}");
        }

        var roleAssignmentResult =
            await _userManager.AddToRoleAsync(
                newUser,
                "SuperAdmin");

        if (!roleAssignmentResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleAssignmentResult.Errors.Select(error =>
                    $"{error.Code}: {error.Description}"));

            throw new InvalidOperationException(
                $"SuperAdmin role assignment failed: {errors}");
        }

        _logger.LogWarning(
            "Development SuperAdmin bootstrap completed for {Email}. " +
            "Disable SuperAdminBootstrap:Enabled after verification.",
            email);
    }
}
