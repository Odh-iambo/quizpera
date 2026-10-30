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

public sealed class NumericEntrySubmissionIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory;
    private readonly HttpClient _client;

    private readonly string _studentId = $"numeric-student-{Guid.NewGuid():N}";
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _questionId = Guid.NewGuid();

    public NumericEntrySubmissionIntegrationTests()
    {
        _factory = new QuizperaTestFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Numeric Entry Test Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Numeric Entry Test Domain",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Numeric Entry Test Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Numeric Entry Test Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var student = new ApplicationUser
        {
            Id = _studentId,
            UserName = $"{_studentId}@example.test",
            Email = $"{_studentId}@example.test"
        };

        var question = new Question
        {
            Id = _questionId,
            Type = QuestionType.NumericEntry,
            Stem = "Calculate the prescribed dose.",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Medium,
            NumericCorrectAnswer = 12.5m,
            NumericTolerance = 0.5m
        };

        var session = new ExamSession
        {
            Id = _sessionId,
            ExamId = exam.Id,
            Exam = exam,
            UserId = student.Id,
            User = student,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.AddRange(program, domain, subject, exam, student, question, session);

        db.ExamQuestions.Add(new ExamQuestion
        {
            ExamId = exam.Id,
            Exam = exam,
            QuestionId = question.Id,
            Question = question,
            DisplayOrder = 1
        });

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private HttpRequestMessage CreateStudentRequest(
        HttpMethod method,
        string url)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            _studentId);

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        return request;
    }

    [Fact]
    public async Task SubmitAnswer_NumericEntry_SavesNumericAnswerWithoutRevealingCorrectness()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId,
            numericAnswer = 12.8m
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseBody);

        Assert.Equal(
            12.8m,
            document.RootElement.GetProperty("numericAnswer").GetDecimal());

        Assert.False(
            document.RootElement.TryGetProperty("isCorrect", out _));

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var savedResponse = await db.ExamSessionResponses
            .SingleAsync(r =>
                r.ExamSessionId == _sessionId &&
                r.QuestionId == _questionId);

        Assert.Equal(12.8m, savedResponse.NumericAnswer);
        Assert.Null(savedResponse.SelectedOptionId);
        Assert.True(savedResponse.IsCorrect);
        Assert.Equal(1m, savedResponse.PointsEarned);
        Assert.Equal(1m, savedResponse.MaximumPoints);
    }

    [Fact]
    public async Task SubmitAnswer_NumericEntry_MissingAnswer_Returns400()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var responseExists = await db.ExamSessionResponses.AnyAsync(r =>
            r.ExamSessionId == _sessionId &&
            r.QuestionId == _questionId);

        Assert.False(responseExists);
    }

    
[Fact]
public async Task Review_NumericEntry_InProgressSession_ReturnsBadRequest()
{
    using var request = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_sessionId}/review");

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task Review_NumericEntry_CompletedSession_ReturnsStudentAndCorrectAnswers()
{
    // Submit the numeric answer.
    using var submitRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{_sessionId}/responses");

    submitRequest.Content = JsonContent.Create(new
    {
        questionId = _questionId,
        numericAnswer = 12.8m
    });

    var submitResponse = await _client.SendAsync(submitRequest);

    Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

    // Complete the exam session.
    using var completeRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{_sessionId}/complete");

    var completeResponse = await _client.SendAsync(completeRequest);

    Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

    // Retrieve the completed exam review.
    using var reviewRequest = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_sessionId}/review");

    var reviewResponse = await _client.SendAsync(reviewRequest);

    Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

    var reviewJson = await reviewResponse.Content.ReadAsStringAsync();

    using var reviewDocument = JsonDocument.Parse(reviewJson);

    var reviewQuestions = reviewDocument.RootElement
        .GetProperty("questions");

    var numericQuestion = reviewQuestions
        .EnumerateArray()
        .Single(q =>
            q.GetProperty("questionId").GetGuid() == _questionId);

    Assert.Equal(
        12.8m,
        numericQuestion.GetProperty("numericAnswer").GetDecimal());

    Assert.Equal(
        12.5m,
        numericQuestion.GetProperty("correctNumericAnswer").GetDecimal());

    Assert.True(
        numericQuestion.GetProperty("isCorrect").GetBoolean());
}

[Fact]
public async Task GetSessionQuestions_NumericEntry_ReturnsQuestionWithoutCorrectAnswer()
{
    using var request = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_sessionId}/questions");

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseBody = await response.Content.ReadAsStringAsync();

    using var document = JsonDocument.Parse(responseBody);

    var questions = document.RootElement
        .GetProperty("questions");

    var question = questions
        .EnumerateArray()
        .Single(q =>
            q.GetProperty("questionId").GetGuid() == _questionId);

    Assert.Equal(
        "NumericEntry",
        question.GetProperty("type").GetString());

    Assert.Equal(
        "Calculate the prescribed dose.",
        question.GetProperty("stem").GetString());

    Assert.False(
        question.TryGetProperty("numericCorrectAnswer", out _));

    Assert.False(
        question.TryGetProperty("numericTolerance", out _));

    Assert.Empty(
        question.GetProperty("options").EnumerateArray());
}

}


