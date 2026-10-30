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

public sealed class RationaleScoringSubmissionIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory = new();
    private HttpClient _client = null!;

    private readonly string _studentId = $"rationale-student-{Guid.NewGuid():N}";
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _questionId = Guid.NewGuid();
    private readonly Guid _correctAnswerId = Guid.NewGuid();
    private readonly Guid _incorrectAnswerId = Guid.NewGuid();
    private readonly Guid _correctRationaleId = Guid.NewGuid();
    private readonly Guid _incorrectRationaleId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Rationale Integration Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Rationale Integration Domain",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Rationale Integration Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Rationale Integration Exam",
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
            Type = QuestionType.SingleChoice,
            ScoringMethod = QuestionScoringMethod.RationaleScoring,
            Stem = "Which intervention is appropriate?",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Medium,
            MaximumPoints = 2m,
            Rationale = "Evaluate the clinical reasoning."
        };

        question.Options.Add(new QuestionOption
        {
            Id = _correctAnswerId,
            QuestionId = question.Id,
            Question = question,
            Text = "Correct intervention",
            DisplayOrder = 1,
            IsCorrect = true
        });

        question.Options.Add(new QuestionOption
        {
            Id = _incorrectAnswerId,
            QuestionId = question.Id,
            Question = question,
            Text = "Incorrect intervention",
            DisplayOrder = 2,
            IsCorrect = false
        });

        question.RationaleOptions.Add(new QuestionRationaleOption
        {
            Id = _correctRationaleId,
            QuestionId = question.Id,
            Question = question,
            Text = "Correct reasoning",
            DisplayOrder = 1,
            IsCorrect = true
        });

        question.RationaleOptions.Add(new QuestionRationaleOption
        {
            Id = _incorrectRationaleId,
            QuestionId = question.Id,
            Question = question,
            Text = "Incorrect reasoning",
            DisplayOrder = 2,
            IsCorrect = false
        });

        var session = new ExamSession
        {
            Id = _sessionId,
            ExamId = exam.Id,
            Exam = exam,
            UserId = student.Id,
            User = student,
            Status = ExamSessionStatus.InProgress,
            Mode = ExamSessionMode.Exam,
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

    private HttpRequestMessage CreateStudentRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(TestAuthenticationHandler.UserIdHeader, _studentId);
        request.Headers.Add(TestAuthenticationHandler.RolesHeader, "Student");
        return request;
    }

    [Fact]
    public async Task SubmitAnswer_RationaleScoring_PersistsAnswerRationaleAndPartialCredit()
    {
        using var request = CreateStudentRequest(
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId,
            selectedOptionId = _correctAnswerId,
            selectedRationaleOptionIds = new[] { _incorrectRationaleId }
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        // Exam Mode must not reveal scoring information.
        Assert.False(body.TryGetProperty("isCorrect", out _));
        Assert.False(body.TryGetProperty("pointsEarned", out _));
        Assert.False(body.TryGetProperty("feedback", out _));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var savedResponse = await db.ExamSessionResponses
            .Include(r => r.SelectedRationaleOptions)
            .SingleAsync(r =>
                r.ExamSessionId == _sessionId &&
                r.QuestionId == _questionId);

        Assert.Equal(_correctAnswerId, savedResponse.SelectedOptionId);
        Assert.True(savedResponse.IsCorrect);
        Assert.Equal(1m, savedResponse.PointsEarned);
        Assert.Equal(2m, savedResponse.MaximumPoints);

        var selectedRationale = Assert.Single(
            savedResponse.SelectedRationaleOptions);

        Assert.Equal(
            _incorrectRationaleId,
            selectedRationale.QuestionRationaleOptionId);
    }


    
[Fact]
public async Task SubmitAnswer_TutoredMode_ReturnsRationaleFeedback()
{
    // Change this test's session to Tutored Mode.
    await using (var scope = _factory.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var session = await db.ExamSessions
            .SingleAsync(s => s.Id == _sessionId);

        session.Mode = ExamSessionMode.Tutored;
        await db.SaveChangesAsync();
    }

    using var request = CreateStudentRequest(
        $"/api/exams/sessions/{_sessionId}/responses");

    request.Content = JsonContent.Create(new
    {
        questionId = _questionId,
        selectedOptionId = _correctAnswerId,
        selectedRationaleOptionIds = new[] { _incorrectRationaleId }
    });

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var document = JsonDocument.Parse(
        await response.Content.ReadAsStringAsync());

    var body = document.RootElement;
    var feedback = body.GetProperty("feedback");

    // The primary answer is correct, but the selected rationale is not.
    Assert.True(feedback.GetProperty("isCorrect").GetBoolean());
    Assert.Equal(1m, feedback.GetProperty("pointsEarned").GetDecimal());
    Assert.Equal(2m, feedback.GetProperty("maximumPoints").GetDecimal());
    Assert.Equal(
        "Evaluate the clinical reasoning.",
        feedback.GetProperty("rationale").GetString());

    // The selected rationale must be returned with its correctness.
    var selectedRationaleOptions = feedback
        .GetProperty("selectedRationaleOptions");

    var selectedRationale = Assert.Single(
        selectedRationaleOptions.EnumerateArray());

    Assert.Equal(
        _incorrectRationaleId,
        selectedRationale.GetProperty("id").GetGuid());

    Assert.Equal(
        "Incorrect reasoning",
        selectedRationale.GetProperty("text").GetString());

    Assert.False(
        selectedRationale.GetProperty("isCorrect").GetBoolean());

    // The correct rationale must also be returned.
    var correctRationaleOptions = feedback
        .GetProperty("correctRationaleOptions");

    var correctRationale = Assert.Single(
        correctRationaleOptions.EnumerateArray());

    Assert.Equal(
        _correctRationaleId,
        correctRationale.GetProperty("id").GetGuid());

    Assert.Equal(
        "Correct reasoning",
        correctRationale.GetProperty("text").GetString());

    Assert.True(
        correctRationale.GetProperty("isCorrect").GetBoolean());
}


        [Fact]
    public async Task SubmitAnswer_RationaleFromAnotherQuestion_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var question = await db.Questions
            .SingleAsync(q => q.Id == _questionId);

        var foreignRationale = new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            Text = "Rationale belonging to another question",
            DisplayOrder = 1,
            IsCorrect = true
        };

        // Add a separate question so the foreign rationale has a real owner.
        var foreignQuestion = new Question
        {
            Id = foreignRationale.QuestionId,
            Type = QuestionType.SingleChoice,
            Stem = "Another question",
            ExamProgramId = question.ExamProgramId,
            DomainId = question.DomainId,
            SubjectId = question.SubjectId,
            Difficulty = Difficulty.Easy
        };

        foreignRationale.Question = foreignQuestion;
        foreignQuestion.RationaleOptions.Add(foreignRationale);

        db.Questions.Add(foreignQuestion);
        await db.SaveChangesAsync();

        using var request = CreateStudentRequest(
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId,
            selectedOptionId = _correctAnswerId,
            selectedRationaleOptionIds = new[] { foreignRationale.Id }
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.False(await db.ExamSessionResponses.AnyAsync(r =>
            r.ExamSessionId == _sessionId &&
            r.QuestionId == _questionId));
    }

    [Fact]
    public async Task SubmitAnswer_RationaleScoringWithoutRationale_ReturnsBadRequest()
    {
        using var request = CreateStudentRequest(
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId,
            selectedOptionId = _correctAnswerId,
            selectedRationaleOptionIds = Array.Empty<Guid>()
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        Assert.False(await db.ExamSessionResponses.AnyAsync(r =>
            r.ExamSessionId == _sessionId &&
            r.QuestionId == _questionId));
    }

    [Fact]
    public async Task SubmitAnswer_CompletedSession_ReturnsBadRequest()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _sessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            $"/api/exams/sessions/{_sessionId}/responses");

        request.Content = JsonContent.Create(new
        {
            questionId = _questionId,
            selectedOptionId = _correctAnswerId,
            selectedRationaleOptionIds = new[] { _correctRationaleId }
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verificationScope =
            _factory.Services.CreateAsyncScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        Assert.False(await verificationDb.ExamSessionResponses.AnyAsync(r =>
            r.ExamSessionId == _sessionId &&
            r.QuestionId == _questionId));
    }
}
