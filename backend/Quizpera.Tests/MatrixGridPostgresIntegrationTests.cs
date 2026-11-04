using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Domain.Auth;
using Microsoft.AspNetCore.SignalR;

namespace Quizpera.Tests;

public sealed class MatrixGridPostgresIntegrationTests : IAsyncLifetime
{
    private readonly PostgresQuizperaTestFactory _factory;
    private readonly HttpClient _client;

    private readonly string _adminId =
        $"postgres-matrix-admin-{Guid.NewGuid():N}";

    private readonly Guid _programId = Guid.NewGuid();
    private readonly Guid _domainId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();

    private readonly string _studentId =
    $"postgres-matrix-student-{Guid.NewGuid():N}";


    public MatrixGridPostgresIntegrationTests()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "QUIZPERA_POSTGRES_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
                throw new InvalidOperationException(
                "Set QUIZPERA_POSTGRES_TEST_CONNECTION before running this test.");
        }

        _factory = new PostgresQuizperaTestFactory(connectionString);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        // Apply the real EF Core migrations to the isolated test database.
        await db.Database.MigrateAsync();

        var program = new ExamProgram
        {
            Id = _programId,
            Name = "PostgreSQL Integration Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = _domainId,
            Name = "PostgreSQL Integration Domain",
            ExamProgramId = _programId,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = _subjectId,
            Name = "PostgreSQL Integration Subject",
            DomainId = _domainId,
            Domain = domain
        };

        var student = new ApplicationUser
{
    Id = _studentId,
    UserName = $"{_studentId}@example.test",
    Email = $"{_studentId}@example.test"
};

db.AddRange(program, domain, subject, student);

await db.SaveChangesAsync();


        
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateMatrixGrid_PersistsRowsColumnsAndAnswersInPostgres()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/questions");

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            _adminId);

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Admin");

        request.Content = JsonContent.Create(new
        {
            type = "MatrixGrid",
            stem = "Identify appropriate nursing interventions.",
            examProgramId = _programId,
            domainId = _domainId,
            subjectId = _subjectId,
            topicId = (Guid?)null,
            clientNeedId = (Guid?)null,
            difficulty = "Medium",
            rationale = "Evaluate each intervention.",
            maximumPoints = 2m,
            scoringMethod = "AllOrNothing",
            options = Array.Empty<object>(),
            
            matrixRows = new[]
            {
                new { text = "Assess vital signs", displayOrder = 1 },
                new { text = "Administer medication", displayOrder = 2 }
            },

            matrixColumns = new[]
            {
                new { text = "Appropriate", displayOrder = 1 },
                new { text = "Not appropriate", displayOrder = 2 }
            },

            matrixAnswers = new[]
            {
                new { rowIndex = 0, correctColumnIndex = 0 },
                new { rowIndex = 1, correctColumnIndex = 1 }
            }


        });

        

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var questionId = document.RootElement
            .GetProperty("id")
            .GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuizperaDbContext>();

        // Clear tracking so assertions read persisted database records.
        db.ChangeTracker.Clear();

        var rows = await db.Set<QuestionMatrixRow>()
            .AsNoTracking()
            .Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var columns = await db.Set<QuestionMatrixColumn>()
            .AsNoTracking()
            .Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        var answers = await db.Set<QuestionMatrixAnswer>()
            .AsNoTracking()
            .Where(x => x.QuestionId == questionId)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, columns.Count);
        Assert.Equal(2, answers.Count);
        Assert.Equal(
            columns[0].Id,
            answers.Single(x => x.MatrixRowId == rows[0].Id)
                .CorrectColumnId);

        Assert.Equal(
            columns[1].Id,
            answers.Single(x => x.MatrixRowId == rows[1].Id)
                .CorrectColumnId);
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
public async Task SubmitMatrixGridAnswer_PersistsStudentSelectionsInPostgres()
{
    // 1. Create a MatrixGrid question through the API.
    using var questionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/questions");

    questionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    questionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    questionRequest.Content = JsonContent.Create(new
    {
        type = "MatrixGrid",
        stem = "Identify appropriate nursing interventions.",
        examProgramId = _programId,
        domainId = _domainId,
        subjectId = _subjectId,
        topicId = (Guid?)null,
        clientNeedId = (Guid?)null,
        difficulty = "Medium",
        rationale = "Evaluate each intervention.",
        maximumPoints = 2m,
        scoringMethod = "AllOrNothing",
        options = Array.Empty<object>(),

        matrixRows = new[]
        {
            new { text = "Assess vital signs", displayOrder = 1 },
            new { text = "Administer medication", displayOrder = 2 }
        },

        matrixColumns = new[]
        {
            new { text = "Appropriate", displayOrder = 1 },
            new { text = "Not appropriate", displayOrder = 2 }
        },

        matrixAnswers = new[]
        {
            new { rowIndex = 0, correctColumnIndex = 0 },
            new { rowIndex = 1, correctColumnIndex = 1 }
        }
    });

    using var questionResponse =
        await _client.SendAsync(questionRequest);

    Assert.Equal(HttpStatusCode.Created, questionResponse.StatusCode);

    using var questionDocument = JsonDocument.Parse(
        await questionResponse.Content.ReadAsStringAsync());

    var questionId = questionDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // 2. Create an exam.
    using var examRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/exams");

    examRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    examRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    examRequest.Content = JsonContent.Create(new
    {
        name = "MatrixGrid Submission Integration Exam",
        type = "Custom",
        examProgramId = _programId
    });

    using var examResponse = await _client.SendAsync(examRequest);

    Assert.Equal(HttpStatusCode.Created, examResponse.StatusCode);

    using var examDocument = JsonDocument.Parse(
        await examResponse.Content.ReadAsStringAsync());

    var examId = examDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // 3. Attach the question to the exam.
    using var addQuestionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"/api/exams/{examId}/questions");

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    addQuestionRequest.Content = JsonContent.Create(new
    {
        questionId
    });

    using var addQuestionResponse =
        await _client.SendAsync(addQuestionRequest);

    Assert.Equal(HttpStatusCode.OK, addQuestionResponse.StatusCode);

    // 4. Start a student exam session.
    using var startSessionRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/{examId}/sessions");

    using var startSessionResponse =
        await _client.SendAsync(startSessionRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        startSessionResponse.StatusCode);

    using var sessionDocument = JsonDocument.Parse(
        await startSessionResponse.Content.ReadAsStringAsync());

    var sessionId = sessionDocument.RootElement
        .GetProperty("sessionId")
        .GetGuid();

    // 5. Retrieve the persisted matrix rows and columns.
    await using var scope = _factory.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<QuizperaDbContext>();

    var rows = await db.Set<QuestionMatrixRow>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    var columns = await db.Set<QuestionMatrixColumn>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    Assert.Equal(2, rows.Count);
    Assert.Equal(2, columns.Count);

    // 6. Submit the student's MatrixGrid answers.
    using var submitRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{sessionId}/responses");

    submitRequest.Content = JsonContent.Create(new
    {
        questionId,

        matrixAnswers = new[]
        {
            new
            {
                matrixRowId = rows[0].Id,
                selectedColumnId = columns[1].Id
            },
            new
            {
                matrixRowId = rows[1].Id,
                selectedColumnId = columns[0].Id
            }
        }
    });

    using var submitResponse = await _client.SendAsync(submitRequest);

    Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

    // 7. Verify the parent response was saved in PostgreSQL.
    var savedResponse = await db.ExamSessionResponses
        .AsNoTracking()
        .SingleAsync(x =>
            x.ExamSessionId == sessionId &&
            x.QuestionId == questionId);

    // 8. Verify the student's row-by-row selections.
    var savedMatrixAnswers = await db.Set<ExamSessionMatrixAnswer>()
        .AsNoTracking()
        .Where(x => x.ExamSessionResponseId == savedResponse.Id)
        .OrderBy(x => x.MatrixRowId)
        .ToListAsync();

    Assert.Equal(2, savedMatrixAnswers.Count);

    Assert.Contains(savedMatrixAnswers, x =>
        x.MatrixRowId == rows[0].Id && 
        x.SelectedColumnId == columns[1].Id);

    Assert.Contains(savedMatrixAnswers, x =>
        x.MatrixRowId == rows[1].Id &&
        x.SelectedColumnId == columns[0].Id);
}

[Fact]
public async Task SubmitMatrixGridAnswer_CorrectAnswers_PersistsFullScoreInPostgres()
{
    // Create a MatrixGrid question.
    using var questionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/questions");

    questionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    questionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    questionRequest.Content = JsonContent.Create(new
    {
        type = "MatrixGrid",
        stem = "Identify appropriate nursing interventions.",
        examProgramId = _programId,
        domainId = _domainId,
        subjectId = _subjectId,
        topicId = (Guid?)null,
        clientNeedId = (Guid?)null,
        difficulty = "Medium",
        rationale = "Evaluate each intervention.",
        maximumPoints = 2m,
        scoringMethod = "AllOrNothing",
        options = Array.Empty<object>(),

        matrixRows = new[]
        {
            new { text = "Assess vital signs", displayOrder = 1 },
            new { text = "Administer medication", displayOrder = 2 }
        },

        matrixColumns = new[]
        {
            new { text = "Appropriate", displayOrder = 1 },
            new { text = "Not appropriate", displayOrder = 2 }
        },

        matrixAnswers = new[]
        {
            new { rowIndex = 0, correctColumnIndex = 0 },
            new { rowIndex = 1, correctColumnIndex = 1 }
        }
    });

    using var questionResponse =
        await _client.SendAsync(questionRequest);

    Assert.Equal(HttpStatusCode.Created, questionResponse.StatusCode);

    using var questionDocument = JsonDocument.Parse(
        await questionResponse.Content.ReadAsStringAsync());

    var questionId = questionDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // Create an exam.
    using var examRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/exams");

    examRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    examRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    examRequest.Content = JsonContent.Create(new
    {
        name = "MatrixGrid Scoring Integration Exam",
        type = "Custom",
        examProgramId = _programId
    });

    using var examResponse = await _client.SendAsync(examRequest);

    Assert.Equal(HttpStatusCode.Created, examResponse.StatusCode);

    using var examDocument = JsonDocument.Parse(
        await examResponse.Content.ReadAsStringAsync());

    var examId = examDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // Attach the question to the exam.
    using var addQuestionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"/api/exams/{examId}/questions");

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    addQuestionRequest.Content = JsonContent.Create(new { questionId });

    using var addQuestionResponse =
        await _client.SendAsync(addQuestionRequest);

    Assert.Equal(HttpStatusCode.OK, addQuestionResponse.StatusCode);

    // Start the student's exam session.
    using var startSessionRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/{examId}/sessions");

    using var startSessionResponse =
        await _client.SendAsync(startSessionRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        startSessionResponse.StatusCode);

    using var sessionDocument = JsonDocument.Parse(
        await startSessionResponse.Content.ReadAsStringAsync());

    var sessionId = sessionDocument.RootElement
        .GetProperty("sessionId")
        .GetGuid();

    // Retrieve the question's matrix rows and columns.
    await using var scope = _factory.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<QuizperaDbContext>();

    var rows = await db.Set<QuestionMatrixRow>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    var columns = await db.Set<QuestionMatrixColumn>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    Assert.Equal(2, rows.Count);
    Assert.Equal(2, columns.Count);

    // Submit both correct answers.
    using var submitRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{sessionId}/responses");

    submitRequest.Content = JsonContent.Create(new
    {
        questionId,

        matrixAnswers = new[]
        {
            new
            {
                matrixRowId = rows[0].Id,
                selectedColumnId = columns[0].Id
            },
            new
            {
                matrixRowId = rows[1].Id,
                selectedColumnId = columns[1].Id
            }
        }
    });

    using var submitResponse = await _client.SendAsync(submitRequest);

    Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

    // Verify the scoring result persisted in PostgreSQL.
    var savedResponse = await db.ExamSessionResponses
        .AsNoTracking()
        .SingleAsync(x =>
            x.ExamSessionId == sessionId &&
            x.QuestionId == questionId);

    Assert.True(savedResponse.IsCorrect);
    Assert.Equal(2m, savedResponse.PointsEarned);
    Assert.Equal(2m, savedResponse.MaximumPoints);
}

[Theory]
[InlineData("AllOrNothing", 0)]
[InlineData("ZeroOne", 1)]
public async Task SubmitMatrixGridAnswer_OneIncorrectRow_PersistsExpectedScoreInPostgres(
    string scoringMethod,
    decimal expectedPoints)
{
    // Create a MatrixGrid question.
    using var questionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/questions");

    questionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    questionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    questionRequest.Content = JsonContent.Create(new
    {
        type = "MatrixGrid",
        stem = "Identify appropriate nursing interventions.",
        examProgramId = _programId,
        domainId = _domainId,
        subjectId = _subjectId,
        topicId = (Guid?)null,
        clientNeedId = (Guid?)null,
        difficulty = "Medium",
        rationale = "Evaluate each intervention.",
        maximumPoints = 2m,
        scoringMethod,
        options = Array.Empty<object>(),

        matrixRows = new[]
        {
            new { text = "Assess vital signs", displayOrder = 1 },
            new { text = "Administer medication", displayOrder = 2 }
        },

        matrixColumns = new[]
        {
            new { text = "Appropriate", displayOrder = 1 },
            new { text = "Not appropriate", displayOrder = 2 }
        },

        matrixAnswers = new[]
        {
            new { rowIndex = 0, correctColumnIndex = 0 },
            new { rowIndex = 1, correctColumnIndex = 1 }
        }
    });

    using var questionResponse =
        await _client.SendAsync(questionRequest);

    Assert.Equal(HttpStatusCode.Created, questionResponse.StatusCode);

    using var questionDocument = JsonDocument.Parse(
        await questionResponse.Content.ReadAsStringAsync());

    var questionId = questionDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // Create an exam.
    using var examRequest = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/exams");

    examRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    examRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    examRequest.Content = JsonContent.Create(new
    {
        name = $"MatrixGrid {scoringMethod} Scoring Exam",
        type = "Custom",
        examProgramId = _programId
    });

    using var examResponse = await _client.SendAsync(examRequest);

    Assert.Equal(HttpStatusCode.Created, examResponse.StatusCode);

    using var examDocument = JsonDocument.Parse(
        await examResponse.Content.ReadAsStringAsync());

    var examId = examDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // Attach the question to the exam.
    using var addQuestionRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"/api/exams/{examId}/questions");

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.UserIdHeader,
        _adminId);

    addQuestionRequest.Headers.Add(
        TestAuthenticationHandler.RolesHeader,
        "Admin");

    addQuestionRequest.Content = JsonContent.Create(new { questionId });

    using var addQuestionResponse =
        await _client.SendAsync(addQuestionRequest);

    Assert.Equal(HttpStatusCode.OK, addQuestionResponse.StatusCode);

    // Start the student's exam session.
    using var startSessionRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/{examId}/sessions");

    using var startSessionResponse =
        await _client.SendAsync(startSessionRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        startSessionResponse.StatusCode);

    using var sessionDocument = JsonDocument.Parse(
        await startSessionResponse.Content.ReadAsStringAsync());

    var sessionId = sessionDocument.RootElement
        .GetProperty("sessionId")
        .GetGuid();

    // Retrieve the matrix rows and columns.
    await using var scope = _factory.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<QuizperaDbContext>();

    var rows = await db.Set<QuestionMatrixRow>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    var columns = await db.Set<QuestionMatrixColumn>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    Assert.Equal(2, rows.Count);
    Assert.Equal(2, columns.Count);

    // First row is correct; second row is deliberately incorrect.
    using var submitRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{sessionId}/responses");

    submitRequest.Content = JsonContent.Create(new
    {
        questionId,

        matrixAnswers = new[]
        {
            new
            {
                matrixRowId = rows[0].Id,
                selectedColumnId = columns[0].Id
            },
            new
            {
                matrixRowId = rows[1].Id,
                selectedColumnId = columns[0].Id
            }
        }
    });

    using var submitResponse = await _client.SendAsync(submitRequest);

    Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

    // Verify the scoring result persisted in PostgreSQL.
    var savedResponse = await db.ExamSessionResponses
        .AsNoTracking()
        .SingleAsync(x =>
            x.ExamSessionId == sessionId &&
            x.QuestionId == questionId);

    Assert.False(savedResponse.IsCorrect);
    Assert.Equal(expectedPoints, savedResponse.PointsEarned);
    Assert.Equal(2m, savedResponse.MaximumPoints);
}
[Fact]
public async Task GetSessionQuestions_RestoresMatrixAnswersWithoutExposingAnswerKey()
{
    // 1. Create a MatrixGrid question.
   using var createQuestionRequest = new HttpRequestMessage(
    HttpMethod.Post,
    "/api/questions");

createQuestionRequest.Headers.Add(
    TestAuthenticationHandler.UserIdHeader,
    _adminId);

createQuestionRequest.Headers.Add(
    TestAuthenticationHandler.RolesHeader,
    "Admin");

    createQuestionRequest.Content = JsonContent.Create(new
    {
        examProgramId = _programId,
        domainId = _domainId,
        subjectId = _subjectId,
        stem = "Assess the patient's clinical findings.",
        type = "MatrixGrid",
        difficulty = "Medium",
        maximumPoints = 2m,
        scoringMethod = "AllOrNothing",
        matrixRows = new[]
        {
            new { text = "Airway", displayOrder = 1 },
            new { text = "Breathing", displayOrder = 2 }
        },
        matrixColumns = new[]
        {
            new { text = "Normal", displayOrder = 1 },
            new { text = "Abnormal", displayOrder = 2 }
        },
        matrixAnswers = new[]
        {
            new { rowIndex = 0, columnIndex = 0 },
            new { rowIndex = 1, columnIndex = 1 }
        }
    });

    using var createQuestionResponse =
        await _client.SendAsync(createQuestionRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        createQuestionResponse.StatusCode);

    using var questionDocument = JsonDocument.Parse(
        await createQuestionResponse.Content.ReadAsStringAsync());

    var questionId = questionDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    // 2. Create an exam and attach the question.
    using var createExamRequest = new HttpRequestMessage(
    HttpMethod.Post,
    "/api/exams");

createExamRequest.Headers.Add(
    TestAuthenticationHandler.UserIdHeader,
    _adminId);

createExamRequest.Headers.Add(
    TestAuthenticationHandler.RolesHeader,
    "Admin");

    createExamRequest.Content = JsonContent.Create(new
    {
        name = "MatrixGrid Recovery Exam",
        type = "Custom",
        examProgramId = _programId
    });

    using var createExamResponse =
        await _client.SendAsync(createExamRequest);

    Assert.Equal(HttpStatusCode.Created, createExamResponse.StatusCode);

    using var examDocument = JsonDocument.Parse(
        await createExamResponse.Content.ReadAsStringAsync());

    var examId = examDocument.RootElement
        .GetProperty("id")
        .GetGuid();

    using var attachQuestionRequest = new HttpRequestMessage(
    HttpMethod.Post,
    $"/api/exams/{examId}/questions");

attachQuestionRequest.Headers.Add(
    TestAuthenticationHandler.UserIdHeader,
    _adminId);

attachQuestionRequest.Headers.Add(
    TestAuthenticationHandler.RolesHeader,
    "Admin");
    attachQuestionRequest.Content = JsonContent.Create(new
    {
        questionId
    });

    using var attachQuestionResponse =
        await _client.SendAsync(attachQuestionRequest);

    Assert.True(
        attachQuestionResponse.IsSuccessStatusCode,
        await attachQuestionResponse.Content.ReadAsStringAsync());

    // 3. Start the student's exam session.
    using var startSessionRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/{examId}/sessions");

    using var startSessionResponse =
        await _client.SendAsync(startSessionRequest);

    Assert.Equal(
        HttpStatusCode.Created,
        startSessionResponse.StatusCode);

    using var sessionDocument = JsonDocument.Parse(
        await startSessionResponse.Content.ReadAsStringAsync());

    var sessionId = sessionDocument.RootElement
        .GetProperty("sessionId")
        .GetGuid();

    // 4. Read the persisted MatrixGrid rows and columns.
    await using var scope = _factory.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<QuizperaDbContext>();

    var rows = await db.Set<QuestionMatrixRow>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    var columns = await db.Set<QuestionMatrixColumn>()
        .AsNoTracking()
        .Where(x => x.QuestionId == questionId)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync();

    Assert.Equal(2, rows.Count);
    Assert.Equal(2, columns.Count);

    // 5. Submit the student's selections.
    using var submitRequest = CreateStudentRequest(
        HttpMethod.Post,
        $"/api/exams/sessions/{sessionId}/responses");

    submitRequest.Content = JsonContent.Create(new
    {
        questionId,
        matrixAnswers = new[]
        {
            new
            {
                matrixRowId = rows[0].Id,
                selectedColumnId = columns[1].Id
            },
            new
            {
                matrixRowId = rows[1].Id,
                selectedColumnId = columns[0].Id
            }
        }
    });

    using var submitResponse = await _client.SendAsync(submitRequest);

    Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

    // 6. Retrieve the questions through the actual recovery endpoint.
    using var recoveryRequest = CreateStudentRequest(
        HttpMethod.Get,
        $"/api/exams/sessions/{sessionId}/questions");

    using var recoveryResponse =
        await _client.SendAsync(recoveryRequest);

    Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);

    using var recoveryDocument = JsonDocument.Parse(
        await recoveryResponse.Content.ReadAsStringAsync());

    var recoveredQuestion = recoveryDocument.RootElement
        .GetProperty("questions")
        .EnumerateArray()
        .Single(x => x.GetProperty("questionId").GetGuid() == questionId);

    // 7. Verify the MatrixGrid structure was recovered.
    Assert.Equal(
        2,
        recoveredQuestion.GetProperty("matrixRows").GetArrayLength());

    Assert.Equal(
        2,
        recoveredQuestion.GetProperty("matrixColumns").GetArrayLength());

    // 8. Verify the student's saved selections were recovered.
    var recoveredAnswers = recoveredQuestion
        .GetProperty("savedAnswer")
        .GetProperty("matrixAnswers")
        .EnumerateArray()
        .ToList();

    Assert.Equal(2, recoveredAnswers.Count);

    Assert.Contains(recoveredAnswers, answer =>
        answer.GetProperty("matrixRowId").GetGuid() == rows[0].Id &&
        answer.GetProperty("selectedColumnId").GetGuid() == columns[1].Id);

    Assert.Contains(recoveredAnswers, answer =>
        answer.GetProperty("matrixRowId").GetGuid() == rows[1].Id &&
        answer.GetProperty("selectedColumnId").GetGuid() == columns[0].Id);

    // 9. Verify the active-exam response does not expose answer keys or scores.
    var responseJson = recoveryDocument.RootElement.GetRawText();

    Assert.DoesNotContain("correctColumnId", responseJson);
    Assert.DoesNotContain("pointsEarned", responseJson);
    Assert.DoesNotContain("maximumPoints", responseJson);
    Assert.DoesNotContain("isCorrect", responseJson);
}

}