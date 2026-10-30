using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Data.Seed;
using Quizpera.Api.Validators;
using Quizpera.Api.Services;
using Quizpera.Api.Services.Evaluation;
using Microsoft.AspNetCore.Identity;
using Quizpera.Api.Domain.Auth;
using Quizpera.Api.Services.Auth;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDataProtection();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-Quizpera.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    };
});



builder.Services.AddDbContext<QuizperaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Quizpera")
    )
);

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<QuizperaDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = "__Host-Quizpera.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Login: maximum 5 requests per IP per minute.
    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Registration: maximum 3 requests per IP per 10 minutes.
    options.AddPolicy("auth-register", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddScoped<CreateQuestionRequestValidator>();
builder.Services.AddOpenApi();

builder.Services.AddScoped<QuestionValidationService>();

builder.Services.AddScoped<IQuestionEvaluator, SingleChoiceEvaluator>();
builder.Services.AddScoped<IQuestionEvaluator, MultipleResponseEvaluator>();
builder.Services.AddScoped<IQuestionEvaluator, NumericEntryEvaluator>();
builder.Services.AddScoped<IQuestionEvaluator, OrderedResponseEvaluator>();
builder.Services.AddScoped<IQuestionEvaluator, TrueFalseEvaluator>();
builder.Services.AddScoped<QuestionEvaluationService>();

builder.Services.AddScoped<ExamSessionResponseService>();
builder.Services.AddScoped<ExamSessionOwnershipService>();
builder.Services.AddScoped<ExamSessionResultService>();
builder.Services.AddScoped<ExamSessionReviewService>();
builder.Services.AddScoped<ExamSessionProgressService>();
builder.Services.AddScoped<ExamSessionQuestionService>();
builder.Services.AddScoped<ExamSessionNavigationService>();
builder.Services.AddScoped<ExamSessionQuestionStatusService>();
builder.Services.AddScoped<ExamSessionQuestionFlagService>();
builder.Services.AddScoped<ExamSessionSubmissionReviewService>();
builder.Services.AddScoped<SuperAdminBootstrapService>();




var app = builder.Build();

var skipStartupSeeding =
    app.Configuration.GetValue<bool>("SkipStartupSeeding");

if (!skipStartupSeeding)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    await IdentityRoleSeed.SeedAsync(services);

    var bootstrap = services.GetRequiredService<
        SuperAdminBootstrapService>();

    await bootstrap.RunAsync();

    var db = services.GetRequiredService<QuizperaDbContext>();

    await QuestionSeed.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();


app.UseAuthentication();

app.Use(async (context, next) =>
{
    var method = context.Request.Method;

    var isUnsafeMethod =
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    var path = context.Request.Path.Value;

    var isAuthenticationEntryPoint =
        string.Equals(
            path,
            "/api/auth/login",
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            path,
            "/api/auth/register",
            StringComparison.OrdinalIgnoreCase);

    if (isUnsafeMethod)
    {
        var cookieAuth = await context.AuthenticateAsync(
            IdentityConstants.ApplicationScheme);

        if (isAuthenticationEntryPoint || cookieAuth.Succeeded)
        {
            var antiforgery = context.RequestServices
                .GetRequiredService<IAntiforgery>();

            if (!await antiforgery.IsRequestValidAsync(context))
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid or missing CSRF token."
                });

                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
