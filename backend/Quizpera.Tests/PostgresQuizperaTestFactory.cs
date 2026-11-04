using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizpera.Api.Data;

namespace Quizpera.Tests;

public sealed class PostgresQuizperaTestFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public PostgresQuizperaTestFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SkipStartupSeeding"] = "true",
                    ["SuperAdminBootstrap:Enabled"] = "false"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<QuizperaDbContext>>();
            services.RemoveAll<DbContextOptions<QuizperaDbContext>>();
            services.RemoveAll<QuizperaDbContext>();

            services.AddDbContext<QuizperaDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", _ => { });
        });
    }
}
