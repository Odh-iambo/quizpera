using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Auth;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamSessionOwnershipIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory;
    private readonly HttpClient _client;

    private readonly string _studentAId =
        $"student-a-{Guid.NewGuid():N}";

    private readonly string _studentBId =
        $"student-b-{Guid.NewGuid():N}";

    private readonly Guid _studentASessionId = Guid.NewGuid();
    private readonly Guid _studentBSessionId = Guid.NewGuid();

    private readonly Guid _questionId = Guid.NewGuid();
    private readonly Guid _optionId = Guid.NewGuid();

    private readonly Guid _outsideQuestionId = Guid.NewGuid();
    private readonly Guid _outsideOptionId = Guid.NewGuid();

    public ExamSessionOwnershipIntegrationTests()
{
    _factory = new QuizperaTestFactory();
    _client = _factory.CreateClient();
}
    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test Program"
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test Exam",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test Domain",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var question = new Question
        {
            Id = _questionId,
            Type = QuestionType.SingleChoice,
            Stem = "Which option is correct?",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Easy
        };

        var option = new QuestionOption
        {
            Id = _optionId,
            QuestionId = question.Id,
            Question = question,
            Text = "Correct option",
            DisplayOrder = 1,
            IsCorrect = true
        };

        // This question belongs to the same program but is not
        // attached to the exam being attempted.
        var outsideQuestion = new Question
        {
            Id = _outsideQuestionId,
            Type = QuestionType.SingleChoice,
            Stem = "A question outside the exam",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Easy
        };

        var outsideOption = new QuestionOption
        {
            Id = _outsideOptionId,
            QuestionId = outsideQuestion.Id,
            Question = outsideQuestion,
            Text = "An option",
            DisplayOrder = 1,
            IsCorrect = true
        };

        var examQuestion = new ExamQuestion
        {
            ExamId = exam.Id,
            Exam = exam,
            QuestionId = question.Id,
            Question = question,
            DisplayOrder = 1
        };

        var studentA = new ApplicationUser
        {
            Id = _studentAId,
            UserName = $"{_studentAId}@example.test",
            Email = $"{_studentAId}@example.test"
        };

        var studentB = new ApplicationUser
        {
            Id = _studentBId,
            UserName = $"{_studentBId}@example.test",
            Email = $"{_studentBId}@example.test"
        };

        var studentASession = new ExamSession
        {
            Id = _studentASessionId,
            ExamId = exam.Id,
            Exam = exam,
            UserId = studentA.Id,
            User = studentA,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        var studentBSession = new ExamSession
        {
            Id = _studentBSessionId,
            ExamId = exam.Id,
            Exam = exam,
            UserId = studentB.Id,
            User = studentB,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.AddRange(
            studentA,
            studentB,
            program,
            domain,
            subject,
            exam,
            question,
            option,
            outsideQuestion,
            outsideOption,
            examQuestion,
            studentASession,
            studentBSession);

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
{
    _client.Dispose();
    await _factory.DisposeAsync();
}
    private HttpRequestMessage CreateStudentRequest(
        HttpMethod method,
        string url,
        string userId)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            userId);

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        return request;
    }

    private static object CreateAnswer(
        Guid questionId,
        Guid optionId)
    {
        return new
        {
            questionId,
            selectedOptionId = optionId
        };
    }

    [Fact]
    public async Task GetSession_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync(
            $"/api/exams/sessions/{_studentASessionId}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetSession_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentBSessionId}",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_OwnSession_Returns200()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentBSessionId}",
            _studentBId);

        var response = await _client.SendAsync(request);

    }

    [Fact]
    public async Task SubmitAnswer_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_studentBSessionId}/responses",
            _studentAId);

        request.Content = JsonContent.Create(
            CreateAnswer(_questionId, _optionId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        var responseExists = await db.ExamSessionResponses.AnyAsync(
            r => r.ExamSessionId == _studentBSessionId
                 && r.QuestionId == _questionId);

        Assert.False(responseExists);
    }

   [Fact]
public async Task SubmitAnswer_OwnSessionAndValidAnswer_Returns200()
{
    using var request = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{_studentBSessionId}/responses",
        _studentBId);

    request.Content = JsonContent.Create(
        CreateAnswer(_questionId, _optionId));

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseBody = await response.Content.ReadAsStringAsync();

    Assert.DoesNotContain(
        "\"isCorrect\"",
        responseBody,
        StringComparison.OrdinalIgnoreCase);

    await using var scope = _factory.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<QuizperaDbContext>();

    var savedResponse = await db.ExamSessionResponses
        .SingleOrDefaultAsync(
            r => r.ExamSessionId == _studentBSessionId
                 && r.QuestionId == _questionId);

    Assert.NotNull(savedResponse);
    Assert.Equal(_optionId, savedResponse.SelectedOptionId);
    Assert.True(savedResponse.IsCorrect);
}

    [Fact]
    public async Task SubmitAnswer_QuestionOutsideExam_Returns400()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_studentBSessionId}/responses",
            _studentBId);

        request.Content = JsonContent.Create(
            CreateAnswer(_outsideQuestionId, _outsideOptionId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        var responseExists = await db.ExamSessionResponses.AnyAsync(
            r => r.ExamSessionId == _studentBSessionId
                 && r.QuestionId == _outsideQuestionId);

        Assert.False(responseExists);
    }


    [Fact]
    public async Task CompleteSession_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_studentBSessionId}/complete",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var session = await db.ExamSessions
            .SingleAsync(s => s.Id == _studentBSessionId);

        Assert.Equal(ExamSessionStatus.InProgress, session.Status);
        Assert.Null(session.CompletedAt);
    }

    [Fact]
    public async Task CompleteSession_OwnActiveSession_Returns200()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_studentBSessionId}/complete",
            _studentBId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);



        var responseBody = await response.Content.ReadAsStringAsync();

Assert.DoesNotContain(
    "\"isCorrect\"",
    responseBody,
    StringComparison.OrdinalIgnoreCase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var session = await db.ExamSessions
            .SingleAsync(s => s.Id == _studentBSessionId);

        Assert.Equal(ExamSessionStatus.Completed, session.Status);
        Assert.NotNull(session.CompletedAt);
    }

    [Fact]
    public async Task CompleteSession_AlreadyCompletedSession_Returns400()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();
            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _studentBSessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Post,
            $"/api/exams/sessions/{_studentBSessionId}/complete",
            _studentBId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }



    [Fact]
    public async Task GetResult_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentBSessionId}/result",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
public async Task GetResult_InProgressSession_Returns404()
{
    using var request = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_studentBSessionId}/result",
        _studentBId);

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task GetResult_OwnCompletedSession_Returns200()
{
    await using (var scope = _factory.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        var session = await db.ExamSessions
            .SingleAsync(s => s.Id == _studentBSessionId);

        session.Status = ExamSessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    using var request = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_studentBSessionId}/result",
        _studentBId);

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}


[Fact]
public async Task GetReview_InProgressSession_Returns400()
{
    using var request = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{_studentBSessionId}/review",
        _studentBId);

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

    [Fact]
    public async Task GetReview_OwnCompletedSession_Returns200()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<QuizperaDbContext>();

            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _studentBSessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentBSessionId}/review",
            _studentBId);

        var response = await _client.SendAsync(request);


        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FlagQuestion_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentBSessionId}/questions/{_questionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagExists = await db.ExamSessionQuestionFlags.AnyAsync(
            f => f.ExamSessionId == _studentBSessionId
                 && f.QuestionId == _questionId);

        Assert.False(flagExists);
    }

    [Fact]
    public async Task FlagQuestion_OwnSession_Returns200AndCreatesFlag()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

Assert.DoesNotContain(
    "\"isCorrect\"",
    responseBody,
    StringComparison.OrdinalIgnoreCase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagCount = await db.ExamSessionQuestionFlags.CountAsync(
            f => f.ExamSessionId == _studentASessionId
                 && f.QuestionId == _questionId);

        Assert.Equal(1, flagCount);
    }

    [Fact]
    public async Task FlagQuestion_QuestionOutsideExam_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_outsideQuestionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagExists = await db.ExamSessionQuestionFlags.AnyAsync(
            f => f.ExamSessionId == _studentASessionId
                 && f.QuestionId == _outsideQuestionId);

        Assert.False(flagExists);
    }

    [Fact]
    public async Task FlagQuestion_AlreadyFlagged_DoesNotCreateDuplicate()
    {
        using var firstRequest = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var firstResponse = await _client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondRequest = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var secondResponse = await _client.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondBody = await secondResponse.Content.ReadFromJsonAsync<FlagResponse>();
        Assert.NotNull(secondBody);
        Assert.False(secondBody.Created);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagCount = await db.ExamSessionQuestionFlags.CountAsync(
            f => f.ExamSessionId == _studentASessionId
                 && f.QuestionId == _questionId);

        Assert.Equal(1, flagCount);
    }

    [Fact]
    public async Task UnflagQuestion_OtherStudentsSession_Returns404()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

            db.ExamSessionQuestionFlags.Add(
                new Quizpera.Api.Domain.Questions.ExamSessionQuestionFlag
                {
                    ExamSessionId = _studentBSessionId,
                    QuestionId = _questionId,
                    FlaggedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Delete,
            $"/api/exams/sessions/{_studentBSessionId}/questions/{_questionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagStillExists = await verifyDb.ExamSessionQuestionFlags.AnyAsync(
            f => f.ExamSessionId == _studentBSessionId
                 && f.QuestionId == _questionId);

        Assert.True(flagStillExists);
    }

    [Fact]
    public async Task FlagAndUnflagQuestion_OwnSession_Succeeds()
    {
        using var flagRequest = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var flagResponse = await _client.SendAsync(flagRequest);
        Assert.Equal(HttpStatusCode.OK, flagResponse.StatusCode);

        using var unflagRequest = CreateStudentRequest(
            HttpMethod.Delete,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var unflagResponse = await _client.SendAsync(unflagRequest);
        Assert.Equal(HttpStatusCode.OK, unflagResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        var flagExists = await db.ExamSessionQuestionFlags.AnyAsync(
            f => f.ExamSessionId == _studentASessionId
                 && f.QuestionId == _questionId);

        Assert.False(flagExists);
    }

    [Fact]
    public async Task GetFlaggedQuestions_OtherStudentsSession_Returns404()
    {
        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentBSessionId}/flagged-questions",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFlaggedQuestions_OwnSession_ReturnsFlaggedQuestion()
    {
        using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

            db.ExamSessionQuestionFlags.Add(
                new Quizpera.Api.Domain.Questions.ExamSessionQuestionFlag
                {
                    ExamSessionId = _studentASessionId,
                    QuestionId = _questionId,
                    FlaggedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentASessionId}/flagged-questions",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<
            List<Quizpera.Api.Contracts.Exams.ExamSessionQuestionStatusResponse>>();

        Assert.NotNull(body);
        var flaggedQuestion = Assert.Single(body);
        Assert.Equal(_questionId, flaggedQuestion.QuestionId);
        Assert.Equal(1, flaggedQuestion.Position);
    }

    private sealed class FlagResponse
    {
        public bool Created { get; set; }
    }



    [Fact]
    public async Task FlagQuestion_CompletedSession_Returns400()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();
            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _studentASessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnflagQuestion_CompletedSession_Returns400()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();
            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _studentASessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            db.ExamSessionQuestionFlags.Add(
                new Quizpera.Api.Domain.Questions.ExamSessionQuestionFlag
                {
                    ExamSessionId = _studentASessionId,
                    QuestionId = _questionId,
                    FlaggedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Delete,
            $"/api/exams/sessions/{_studentASessionId}/questions/{_questionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

        Assert.True(await verifyDb.ExamSessionQuestionFlags.AnyAsync(
            f => f.ExamSessionId == _studentASessionId
                 && f.QuestionId == _questionId));
    }

    [Fact]
    public async Task FlagQuestion_NonexistentQuestion_Returns404()
    {
        var nonexistentQuestionId = Guid.NewGuid();

        using var request = CreateStudentRequest(
            HttpMethod.Put,
            $"/api/exams/sessions/{_studentASessionId}/questions/{nonexistentQuestionId}/flag",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFlaggedQuestions_CompletedSession_Returns400()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();
            var session = await db.ExamSessions
                .SingleAsync(s => s.Id == _studentASessionId);

            session.Status = ExamSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        using var request = CreateStudentRequest(
            HttpMethod.Get,
            $"/api/exams/sessions/{_studentASessionId}/flagged-questions",
            _studentAId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
