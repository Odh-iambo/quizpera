using Microsoft.AspNetCore.Identity;

namespace Quizpera.Api.Data.Seed;

public static class IdentityRoleSeed
{
    private static readonly string[] Roles =
    [
        "Student",
        "Admin",
        "SuperAdmin"
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create the '{role}' role: {errors}");
            }
        }
    }
}
