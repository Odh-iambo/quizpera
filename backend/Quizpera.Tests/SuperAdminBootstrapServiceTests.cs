using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Auth;
using Quizpera.Api.Services.Auth;

namespace Quizpera.Tests;

public sealed class SuperAdminBootstrapServiceTests
{
    [Fact]
    public async Task RunAsync_WhenBootstrapDisabled_DoesNotCreateAccount()
    {
        // Arrange
        await using var test = await CreateTestContextAsync(
            "Development",
            new Dictionary<string, string?>
            {
                ["SuperAdminBootstrap:Enabled"] = "false",
                ["SuperAdminBootstrap:Email"] = "bootstrap@example.com",
                ["SuperAdminBootstrap:Password"] = "StrongPassword123!"
            });

        // Act
        await test.Service.RunAsync();

        // Assert
        var user = await test.UserManager.FindByEmailAsync(
            "bootstrap@example.com");

        Assert.Null(user);
    }

    [Fact]
    public async Task RunAsync_InProduction_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var test = await CreateTestContextAsync(
            "Production",
            new Dictionary<string, string?>
            {
                ["SuperAdminBootstrap:Enabled"] = "true",
                ["SuperAdminBootstrap:Email"] = "bootstrap@example.com",
                ["SuperAdminBootstrap:Password"] = "StrongPassword123!"
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.RunAsync());

        // Assert
        Assert.Equal(
            "SuperAdmin bootstrap is only permitted in Development.",
            exception.Message);
    }

    [Fact]
    public async Task RunAsync_WhenCredentialsAreMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var test = await CreateTestContextAsync(
            "Development",
            new Dictionary<string, string?>
            {
                ["SuperAdminBootstrap:Enabled"] = "true",
                ["SuperAdminBootstrap:Email"] = "",
                ["SuperAdminBootstrap:Password"] = ""
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.RunAsync());

        // Assert
        Assert.Equal(
            "SuperAdmin bootstrap requires an email and password.",
            exception.Message);
    }

[Fact]
public async Task RunAsync_WhenExistingStudentMatchesBootstrapEmail_RefusesPromotion()
{
    // Arrange
    const string email = "student@example.com";

    await using var test = await CreateTestContextAsync(
        "Development",
        new Dictionary<string, string?>
        {
            ["SuperAdminBootstrap:Enabled"] = "true",
            ["SuperAdminBootstrap:Email"] = email,
            ["SuperAdminBootstrap:Password"] = "StrongPassword123!"
        });

    var roleResult = await test.RoleManager.CreateAsync(
        new IdentityRole("SuperAdmin"));

    Assert.True(roleResult.Succeeded);

    var student = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true
    };

    var createResult = await test.UserManager.CreateAsync(
        student,
        "StudentPassword123!");

    Assert.True(createResult.Succeeded);

    // Act
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => test.Service.RunAsync());

    // Assert
    Assert.Equal(
        "A SuperAdmin bootstrap account already exists. " +
        "Refusing to promote an existing account.",
        exception.Message);

    Assert.False(
        await test.UserManager.IsInRoleAsync(student, "SuperAdmin"));
}
  
    private static async Task<BootstrapTestContext> CreateTestContextAsync(
        string environmentName,
        Dictionary<string, string?> settings)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<QuizperaDbContext>(options =>
            options.UseSqlite(connection));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<QuizperaDbContext>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();

        try
        {
            var db = scope.ServiceProvider
                .GetRequiredService<QuizperaDbContext>();

            await db.Database.EnsureCreatedAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var environment = new TestHostEnvironment(environmentName);

            var service = new SuperAdminBootstrapService(
                scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>(),
                configuration,
                environment,
                scope.ServiceProvider
                    .GetRequiredService<ILogger<SuperAdminBootstrapService>>());

            return new BootstrapTestContext(
                connection,
                provider,
                scope,
                service);
        }
        catch
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "Quizpera.Tests";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class BootstrapTestContext : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;

        public BootstrapTestContext(
            SqliteConnection connection,
            ServiceProvider provider,
            AsyncServiceScope scope,
            SuperAdminBootstrapService service)
        {
            _connection = connection;
            _provider = provider;
            _scope = scope;
            Service = service;
        }

        public SuperAdminBootstrapService Service { get; }

        public UserManager<ApplicationUser> UserManager =>
            _scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

                public RoleManager<IdentityRole> RoleManager =>
    _scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
