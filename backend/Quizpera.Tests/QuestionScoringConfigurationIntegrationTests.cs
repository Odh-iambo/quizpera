using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class QuestionScoringConfigurationIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory = new();
    private readonly HttpClient _client;

    private readonly string _adminId = $"scoring-admin-{Guid.NewGuid():N}";

    private readonly Guid _programId = Guid.NewGuid();
    private readonly Guid _domainId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();

    public QuestionScoringConfigurationIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var program = new ExamProgram
        {
            Id = _programId,
            Name = "Scoring Integration Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = _domainId,
            Name = "Scoring Integration Domain",
            ExamProgramId = _programId,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = _subjectId,
            Name = "Scoring Integration Subject",
            DomainId = _domainId,
            Domain = domain
        };

        db.AddRange(program, domain, subject);

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private HttpRequestMessage CreateAdminRequest(
        HttpMethod method,
        string url)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            _adminId);

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Admin");

        return request;
    }

    private object CreateQuestionPayload(
        string type,
        object scoringMethod,
        decimal maximumPoints = 1m)
    {
        return new
        {
            type,
            stem = "Which findings require follow-up?",
            examProgramId = _programId,
            domainId = _domainId,
            subjectId = _subjectId,
            topicId = (Guid?)null,
            clientNeedId = (Guid?)null,
            difficulty = "Medium",
            rationale = "Review the clinical findings.",
            maximumPoints,
            scoringMethod,
            options = new[]
            {
                new
                {
                    text = "Finding A",
                    displayOrder = 1,
                    correctOrder = (int?)null,
                    isCorrect = true
                },
                new
                {
                    text = "Finding B",
                    displayOrder = 2,
                    correctOrder = (int?)null,
                    isCorrect = true
                },
                new
                {
                    text = "Finding C",
                    displayOrder = 3,
                    correctOrder = (int?)null,
                    isCorrect = false
                }
            }
        };
    }

    [Fact]
    public async Task CreateQuestion_PersistsAndReturnsScoringConfiguration()
    {
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/api/questions");

        request.Content = JsonContent.Create(new
        {
            type = "MultipleResponse",
            stem = "Which findings require immediate follow-up?",
            examProgramId = _programId,
            domainId = _domainId,
            subjectId = _subjectId,
            topicId = (Guid?)null,
            clientNeedId = (Guid?)null,
            difficulty = "Medium",
            rationale = "Evaluate the patient's current condition.",
            maximumPoints = 3m,
            scoringMethod = "PlusMinus",
            options = new[]
            {
                new
                {
                    text = "Finding A",
                    displayOrder = 1,
                    correctOrder = (int?)null,
                    isCorrect = true
                },
                new
                {
                    text = "Finding B",
                    displayOrder = 2,
                    correctOrder = (int?)null,
                    isCorrect = true
                },
                new
                {
                    text = "Finding C",
                    displayOrder = 3,
                    correctOrder = (int?)null,
                    isCorrect = false
                }
            }
        });

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseBody);

        var root = document.RootElement;
        var questionId = root.GetProperty("id").GetGuid();

        Assert.Equal(
            3m,
            root.GetProperty("maximumPoints").GetDecimal());

        Assert.Equal(
            "PlusMinus",
            root.GetProperty("scoringMethod").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var savedQuestion = await db.Questions
            .SingleAsync(question => question.Id == questionId);

        Assert.Equal(3m, savedQuestion.MaximumPoints);
        Assert.Equal(
            QuestionScoringMethod.PlusMinus,
            savedQuestion.ScoringMethod);

        using var getRequest = CreateAdminRequest(
            HttpMethod.Get,
            $"/api/questions/{questionId}");

        using var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getBody = await getResponse.Content.ReadAsStringAsync();

        using var getDocument = JsonDocument.Parse(getBody);

        var getRoot = getDocument.RootElement;

        Assert.Equal(
            3m,
            getRoot.GetProperty("maximumPoints").GetDecimal());

        Assert.Equal(
            "PlusMinus",
            getRoot.GetProperty("scoringMethod").GetString());
    }

    [Fact]
    public async Task CreateQuestion_WithZeroOne_PersistsAndReturnsConfiguration()
    {
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/api/questions");

        request.Content = JsonContent.Create(
            CreateQuestionPayload("MultipleResponse", "ZeroOne"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        var root = document.RootElement;
        var questionId = root.GetProperty("id").GetGuid();

        Assert.Equal(
            "ZeroOne",
            root.GetProperty("scoringMethod").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var savedQuestion = await db.Questions
            .SingleAsync(question => question.Id == questionId);

        Assert.Equal(
            QuestionScoringMethod.ZeroOne,
            savedQuestion.ScoringMethod);

        Assert.Equal(1m, savedQuestion.MaximumPoints);

        using var getRequest = CreateAdminRequest(
            HttpMethod.Get,
            $"/api/questions/{questionId}");

        using var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getBody = await getResponse.Content.ReadAsStringAsync();

        using var getDocument = JsonDocument.Parse(getBody);

        Assert.Equal(
            "ZeroOne",
            getDocument.RootElement
                .GetProperty("scoringMethod")
                .GetString());
    }

    [Fact]
    public async Task CreateQuestion_SingleChoiceWithZeroOne_ReturnsBadRequest()
    {
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/api/questions");

        request.Content = JsonContent.Create(
            CreateQuestionPayload("SingleChoice", "ZeroOne"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_WithUndefinedScoringMethod_ReturnsBadRequest()
    {
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/api/questions");

        request.Content = JsonContent.Create(
            CreateQuestionPayload("MultipleResponse", 99));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
