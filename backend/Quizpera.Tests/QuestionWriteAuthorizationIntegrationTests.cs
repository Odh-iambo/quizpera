using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Quizpera.Tests;

public sealed class QuestionWriteAuthorizationIntegrationTests : IDisposable
{
    private readonly QuizperaTestFactory _factory = new();
    private readonly HttpClient _client;

    public QuestionWriteAuthorizationIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateQuestion_WithoutAuthentication_Returns401()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/questions",
            ValidQuestionRequest());

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_AsStudent_Returns403()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/questions")
        {
            Content = JsonContent.Create(ValidQuestionRequest())
        };

        AddIdentityHeaders(request, "Student");

        using var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task EvaluateAnswer_AsStudent_Returns403()
    {
        var questionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/questions/{questionId}/evaluate")
        {
            Content = JsonContent.Create(new
            {
                QuestionId = questionId,
                SelectedOptionId = Guid.NewGuid()
            })
        };

        AddIdentityHeaders(request, "Student");

        using var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetQuestionReview_WithoutAuthentication_Returns401()
    {
        using var response = await _client.GetAsync(
            $"/api/questions/{Guid.NewGuid()}/review");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

[Fact]
public async Task GetQuestionReview_AsStudent_Returns403()
{
    using var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/questions/{Guid.NewGuid()}/review");

    AddIdentityHeaders(request, "Student");

    using var response = await _client.SendAsync(request);

    Assert.Equal(
        HttpStatusCode.Forbidden,
        response.StatusCode);
}


    private static object ValidQuestionRequest() => new
    {
        Type = "SingleChoice",
        Stem = "Which action best prevents infection?",
        ExamProgramId = Guid.NewGuid(),
        DomainId = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        TopicId = (Guid?)null,
        ClientNeedId = (Guid?)null,
        Difficulty = "Easy",
        Rationale = "Hand hygiene helps reduce transmission.",
        Options = new[]
        {
            new
            {
                Text = "Perform hand hygiene",
                DisplayOrder = 1,
                IsCorrect = true
            },
            new
            {
                Text = "Skip hand hygiene",
                DisplayOrder = 2,
                IsCorrect = false
            }
        }
    };

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
