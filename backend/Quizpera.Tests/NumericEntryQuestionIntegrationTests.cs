using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Auth;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class NumericEntryQuestionIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory = new();
    private readonly HttpClient _client;

    private readonly string _adminId = $"numeric-admin-{Guid.NewGuid():N}";

    private readonly Guid _programId = Guid.NewGuid();
    private readonly Guid _domainId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();

    public NumericEntryQuestionIntegrationTests()
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
            Name = "Numeric Integration Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = _domainId,
            Name = "Numeric Integration Domain",
            ExamProgramId = _programId,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = _subjectId,
            Name = "Numeric Integration Subject",
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

    [Fact]
    public async Task CreateQuestion_NumericEntry_PersistsCorrectAnswerAndTolerance()
    {
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            "/api/questions");

        request.Content = JsonContent.Create(new
        {
            type = "NumericEntry",
            stem = "Calculate the prescribed dose.",
            examProgramId = _programId,
            domainId = _domainId,
            subjectId = _subjectId,
            topicId = (Guid?)null,
            clientNeedId = (Guid?)null,
            difficulty = "Medium",
            rationale = "Use the prescribed calculation.",
            numericCorrectAnswer = 12.5m,
            numericTolerance = 0.5m,
            options = Array.Empty<object>()
        });

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseBody);

        var root = document.RootElement;

        var questionId = root.GetProperty("id").GetGuid();

        Assert.Equal(
            "NumericEntry",
            root.GetProperty("type").GetString());

            Assert.Equal(
    12.5m,
    root.GetProperty("numericCorrectAnswer").GetDecimal());

Assert.Equal(
    0.5m,
    root.GetProperty("numericTolerance").GetDecimal());

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var savedQuestion = await db.Questions
            .SingleAsync(q => q.Id == questionId);

        Assert.Equal(QuestionType.NumericEntry, savedQuestion.Type);
        Assert.Equal(12.5m, savedQuestion.NumericCorrectAnswer);
        Assert.Equal(0.5m, savedQuestion.NumericTolerance);

        Assert.Empty(
            await db.QuestionOptions
                .Where(o => o.QuestionId == questionId)
                .ToListAsync());

                using var getQuestionRequest = CreateAdminRequest(
    HttpMethod.Get,
    $"/api/questions/{questionId}");

using var getQuestionResponse =
    await _client.SendAsync(getQuestionRequest);

Assert.Equal(
    HttpStatusCode.OK,
    getQuestionResponse.StatusCode);

var getQuestionBody =
    await getQuestionResponse.Content.ReadAsStringAsync();

using var getQuestionDocument =
    JsonDocument.Parse(getQuestionBody);

var getQuestionRoot = getQuestionDocument.RootElement;

Assert.Equal(
    12.5m,
    getQuestionRoot.GetProperty("numericCorrectAnswer").GetDecimal());

Assert.Equal(
    0.5m,
    getQuestionRoot.GetProperty("numericTolerance").GetDecimal());


    using var reviewRequest = CreateAdminRequest(
    HttpMethod.Get,
    $"/api/questions/{questionId}/review");

using var reviewResponse =
    await _client.SendAsync(reviewRequest);

Assert.Equal(
    HttpStatusCode.OK,
    reviewResponse.StatusCode);

var reviewBody =
    await reviewResponse.Content.ReadAsStringAsync();

using var reviewDocument = JsonDocument.Parse(reviewBody);

var reviewRoot = reviewDocument.RootElement;

Assert.Equal(
    12.5m,
    reviewRoot.GetProperty("numericCorrectAnswer").GetDecimal());

Assert.Equal(
    0.5m,
    reviewRoot.GetProperty("numericTolerance").GetDecimal());
    }
}
