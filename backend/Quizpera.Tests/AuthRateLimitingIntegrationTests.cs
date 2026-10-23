using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Tests;

public sealed class AuthRateLimitingIntegrationTests
{



    [Fact]
public async Task AuthenticatedIdentityCookie_AnswerWithoutCsrfToken_Returns400()
{
    await using var factory = new QuizperaTestFactory();

    await SeedStudentRoleAsync(factory);

    using var client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    var email = $"csrf-student-{Guid.NewGuid():N}@example.com";
    const string password = "StrongPassword123!";

    // Obtain the CSRF token needed for registration and login.
    var csrfToken = await GetCsrfTokenAsync(client);

    using var registerRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/auth/register")
    {
        Content = JsonContent.Create(new
        {
            email,
            password
        })
    };

    registerRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var registerResponse = await client.SendAsync(registerRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        registerResponse.StatusCode);

    // Log in through the real Identity login endpoint.
    using var loginRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/auth/login")
    {
        Content = JsonContent.Create(new
        {
            email,
            password
        })
    };

    loginRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var loginResponse = await client.SendAsync(loginRequest);

    Assert.Equal(
        HttpStatusCode.OK,
        loginResponse.StatusCode);

    // Confirm that the actual Identity authentication cookie was issued.
    Assert.Contains(
        loginResponse.Headers.GetValues("Set-Cookie"),
        cookie => cookie.StartsWith(
            "__Host-Quizpera.Auth=",
            StringComparison.Ordinal));

    // Seed a valid exam session for the registered student.
var sessionId = Guid.NewGuid();
var examId = Guid.NewGuid();
var programId = Guid.NewGuid();
var domainId = Guid.NewGuid();
var subjectId = Guid.NewGuid();
var questionId = Guid.NewGuid();
var optionId = Guid.NewGuid();

await using (var scope = factory.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

    var user = await db.Users.SingleAsync(u => u.Email == email);

    var program = new ExamProgram
    {
        Id = programId,
        Name = "CSRF Integration Test Program"
    };

    var exam = new Exam
    {
        Id = examId,
        Name = "CSRF Integration Test Exam",
        Type = ExamType.Custom,
        ExamProgramId = programId,
        ExamProgram = program
    };

    var domain = new Quizpera.Api.Domain.Questions.Domain
    {
        Id = domainId,
        Name = "CSRF Integration Test Domain",
        ExamProgramId = programId,
        ExamProgram = program
    };

    var subject = new Subject
    {
        Id = subjectId,
        Name = "CSRF Integration Test Subject",
        DomainId = domainId,
        Domain = domain
    };

    var question = new Question
    {
        Id = questionId,
        Type = QuestionType.SingleChoice,
        Stem = "CSRF test question",
        ExamProgramId = programId,
        ExamProgram = program,
        DomainId = domainId,
        Domain = domain,
        SubjectId = subjectId,
        Subject = subject,
        Difficulty = Difficulty.Easy
    };

    var option = new QuestionOption
    {
        Id = optionId,
        QuestionId = questionId,
        Question = question,
        Text = "Test option",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var examQuestion = new ExamQuestion
    {
        ExamId = examId,
        Exam = exam,
        QuestionId = questionId,
        Question = question,
        DisplayOrder = 1
    };

    var session = new ExamSession
    {
        Id = sessionId,
        ExamId = examId,
        Exam = exam,
        UserId = user.Id,
        User = user,
        Status = ExamSessionStatus.InProgress,
        StartedAt = DateTime.UtcNow
    };

    db.AddRange(
        program,
        exam,
        domain,
        subject,
        question,
        option,
        examQuestion,
        session);

    await db.SaveChangesAsync();
}

// Attempt to submit a valid answer without the CSRF request-token header.
using var answerRequest = new HttpRequestMessage(
    HttpMethod.Post,
    $"/api/exams/sessions/{sessionId}/responses")
{
    Content = JsonContent.Create(new
    {
        questionId,
        selectedOptionId = optionId
    })
};



    // Deliberately do not add the X-CSRF-TOKEN header.
    // HandleCookies=true allows the client to send its stored cookies.
    using var answerResponse = await client.SendAsync(answerRequest);

    Assert.Equal(
        HttpStatusCode.BadRequest,
        answerResponse.StatusCode);

    var body = await answerResponse.Content.ReadAsStringAsync();

    Assert.Contains(
        "Invalid or missing CSRF token.",
        body,
        StringComparison.Ordinal);



        // Confirm that the rejected request did not persist an answer.
await using var verifyScope = factory.Services.CreateAsyncScope();

var verifyDb = verifyScope.ServiceProvider
    .GetRequiredService<QuizperaDbContext>();

var responseExists = await verifyDb.ExamSessionResponses.AnyAsync(
    r => r.ExamSessionId == sessionId
         && r.QuestionId == questionId);

Assert.False(responseExists);
}



    [Fact]
public async Task GetCsrfToken_ReturnsTokenAndAntiforgeryCookie()
{
    await using var factory = new QuizperaTestFactory();

    using var client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    using var response = await client.GetAsync("/api/auth/csrf");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var document = JsonDocument.Parse(
        await response.Content.ReadAsStringAsync());

    Assert.True(
        document.RootElement.TryGetProperty(
            "requestToken",
            out var tokenElement));

    Assert.False(string.IsNullOrWhiteSpace(tokenElement.GetString()));

    Assert.Contains(
        response.Headers.GetValues("Set-Cookie"),
        cookie => cookie.StartsWith(
            "__Host-Quizpera.Csrf=",
            StringComparison.Ordinal));
}

[Fact]
public async Task Login_WithoutCsrfToken_Returns400()
{
    await using var factory = new QuizperaTestFactory();

    using var client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    using var response = await client.PostAsJsonAsync(
        "/api/auth/login",
        new
        {
            email = "invalid-user@example.com",
            password = "WrongPassword123!"
        });

    Assert.Equal(
        HttpStatusCode.BadRequest,
        response.StatusCode);

    var body = await response.Content.ReadAsStringAsync();

    Assert.Contains(
        "Invalid or missing CSRF token.",
        body,
        StringComparison.Ordinal);
}

[Fact]
public async Task Login_WithValidCsrfTokenAndInvalidCredentials_Returns401()
{
    await using var factory = new QuizperaTestFactory();

    using var client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    var csrfToken = await GetCsrfTokenAsync(client);

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/auth/login")
    {
        Content = JsonContent.Create(new
        {
            email = "invalid-user@example.com",
            password = "WrongPassword123!"
        })
    };

    request.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var response = await client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}

	[Fact]
public async Task Logout_WithValidCsrfToken_ClearsAuthentication()
{
    await using var factory = new QuizperaTestFactory(
    useIdentityAuthentication: true);

    await SeedStudentRoleAsync(factory);

    using var client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    var email = $"logout-{Guid.NewGuid():N}@example.com";
    const string password = "StrongPassword123!";

    var csrfToken = await GetCsrfTokenAsync(client);

    using var registerRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/auth/register")
    {
        Content = JsonContent.Create(new { email, password })
    };

    registerRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var registerResponse = await client.SendAsync(registerRequest);

    Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

    using var loginRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/auth/login")
    {
        Content = JsonContent.Create(new { email, password })
    };

    loginRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var loginResponse = await client.SendAsync(loginRequest);

    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

   using var meBeforeLogout = await client.GetAsync("/api/auth/me");

Assert.Equal(HttpStatusCode.OK, meBeforeLogout.StatusCode);

// Obtain a CSRF token for the authenticated user.
csrfToken = await GetCsrfTokenAsync(client);

using var logoutRequest = new HttpRequestMessage(
    HttpMethod.Post,
    "/api/auth/logout");

logoutRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

    using var logoutResponse = await client.SendAsync(logoutRequest);

    Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

    using var meAfterLogout = await client.GetAsync("/api/auth/me");

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        meAfterLogout.StatusCode);
}


    [Fact]
    public async Task Login_SixthRequestFromSameIp_Returns429()
    {
        await using var factory = new QuizperaTestFactory();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });


        var csrfToken = await GetCsrfTokenAsync(client);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        for (var i = 0; i < 5; i++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = $"missing-{Guid.NewGuid():N}@example.com",
                    password = "InvalidPassword123!"
                });

            Assert.NotEqual(
                HttpStatusCode.TooManyRequests,
                response.StatusCode);
        }

        using var sixthResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = $"missing-{Guid.NewGuid():N}@example.com",
                password = "InvalidPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            sixthResponse.StatusCode);
    }

    [Fact]
    public async Task Registration_FourthRequestFromSameIp_Returns429()
    {
        await using var factory = new QuizperaTestFactory();
        await SeedStudentRoleAsync(factory);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });

        var csrfToken = await GetCsrfTokenAsync(client);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        for (var i = 0; i < 3; i++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    email = $"register-{Guid.NewGuid():N}@example.com",
                    password = "StrongPassword123!"
                });

            Assert.NotEqual(
                HttpStatusCode.TooManyRequests,
                response.StatusCode);
        }

        using var fourthResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"register-{Guid.NewGuid():N}@example.com",
                password = "StrongPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            fourthResponse.StatusCode);
    }

    [Fact]
    public async Task LoginLimitExceeded_DoesNotConsumeRegistrationLimit()
    {
        await using var factory = new QuizperaTestFactory();
        await SeedStudentRoleAsync(factory);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });

        var csrfToken = await GetCsrfTokenAsync(client);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        for (var i = 0; i < 6; i++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = $"missing-{Guid.NewGuid():N}@example.com",
                    password = "InvalidPassword123!"
                });

            if (i < 5)
            {
                Assert.NotEqual(
                    HttpStatusCode.TooManyRequests,
                    response.StatusCode);
            }
            else
            {
                Assert.Equal(
                    HttpStatusCode.TooManyRequests,
                    response.StatusCode);
            }
        }

        using var registrationResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"independent-{Guid.NewGuid():N}@example.com",
                password = "StrongPassword123!"
            });

        Assert.NotEqual(
            HttpStatusCode.TooManyRequests,
            registrationResponse.StatusCode);
    }




    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var document = System.Text.Json.JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement
            .GetProperty("requestToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "The CSRF endpoint returned no request token.");
    }

    private static async Task SeedStudentRoleAsync(
        QuizperaTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Student"))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole("Student"));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not seed Student role for rate-limit tests: "
                    + string.Join(
                        "; ",
                        result.Errors.Select(error => error.Description)));
            }
        }
    }

}
