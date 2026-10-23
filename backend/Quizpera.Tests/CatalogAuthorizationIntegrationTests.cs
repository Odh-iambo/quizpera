using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class CatalogAuthorizationIntegrationTests : IDisposable
{
    private readonly QuizperaTestFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogAuthorizationIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/domains")]
    [InlineData("/api/subjects")]
    [InlineData("/api/topics")]
    [InlineData("/api/examprograms")]
    public async Task CreateCatalogItem_WithoutAuthentication_Returns401(
        string endpoint)
    {
        using var response = await _client.PostAsJsonAsync(
            endpoint,
            new { Name = "Authorization Test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/domains")]
    [InlineData("/api/subjects")]
    [InlineData("/api/topics")]
    [InlineData("/api/examprograms")]
    public async Task CreateCatalogItem_AsStudent_Returns403(
        string endpoint)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint)
        {
            Content = JsonContent.Create(new
            {
                Name = "Authorization Test",
                ExamProgramId = Guid.NewGuid(),
                DomainId = Guid.NewGuid(),
                SubjectId = Guid.NewGuid()
            })
        };

        AddIdentityHeaders(request, "Student");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateExamProgram_AsAdmin_Returns201()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/examprograms")
        {
            Content = JsonContent.Create(new
            {
                Name = $"Admin Test Program {Guid.NewGuid():N}"
            })
        };

        AddIdentityHeaders(request, "Admin");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateDomain_AsAdmin_Returns201()
    {
        var programId = await SeedExamProgramAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/domains")
        {
            Content = JsonContent.Create(new
            {
                Name = $"Admin Test Domain {Guid.NewGuid():N}",
                ExamProgramId = programId
            })
        };

        AddIdentityHeaders(request, "Admin");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_AsAdmin_Returns201()
    {
        var domainId = await SeedDomainAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/subjects")
        {
            Content = JsonContent.Create(new
            {
                Name = $"Admin Test Subject {Guid.NewGuid():N}",
                DomainId = domainId
            })
        };

        AddIdentityHeaders(request, "Admin");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_AsAdmin_Returns201()
    {
        var subjectId = await SeedSubjectAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/topics")
        {
            Content = JsonContent.Create(new
            {
                Name = $"Admin Test Topic {Guid.NewGuid():N}",
                SubjectId = subjectId
            })
        };

        AddIdentityHeaders(request, "Admin");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> SeedExamProgramAsync()
    {
        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = $"Seed Program {Guid.NewGuid():N}"
        };

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        db.ExamPrograms.Add(program);
        await db.SaveChangesAsync();

        return program.Id;
    }

    private async Task<Guid> SeedDomainAsync()
    {
        var programId = await SeedExamProgramAsync();

        var domain = new Domain
        {
            Id = Guid.NewGuid(),
            Name = $"Seed Domain {Guid.NewGuid():N}",
            ExamProgramId = programId
        };

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        db.Domains.Add(domain);
        await db.SaveChangesAsync();

        return domain.Id;
    }

    private async Task<Guid> SeedSubjectAsync()
    {
        var domainId = await SeedDomainAsync();

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = $"Seed Subject {Guid.NewGuid():N}",
            DomainId = domainId
        };

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        return subject.Id;
    }

    private static void AddIdentityHeaders(
        HttpRequestMessage request,
        string role)
    {
        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            $"test-{Guid.NewGuid():N}");

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            role);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
