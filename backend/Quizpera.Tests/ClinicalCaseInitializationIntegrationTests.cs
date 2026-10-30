using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class ClinicalCaseInitializationIntegrationTests : IAsyncLifetime
{
    private readonly QuizperaTestFactory _factory = new();
    private readonly HttpClient _client;

    private readonly string _studentId = $"ngn-student-{Guid.NewGuid():N}";

    public ClinicalCaseInitializationIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task StartExam_PublishedClinicalCase_InitializesFirstStage()
    {
        var programId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var firstStageId = Guid.NewGuid();
        var secondStageId = Guid.NewGuid();
        var firstQuestionId = Guid.NewGuid();
        var secondQuestionId = Guid.NewGuid();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

            var program = new ExamProgram
            {
                Id = programId,
                Name = "NGN Integration Program"
            };

            var domain = new Quizpera.Api.Domain.Questions.Domain
            {
                Id = domainId,
                Name = "Clinical Judgment",
                ExamProgramId = programId,
                ExamProgram = program
            };

            var subject = new Subject
            {
                Id = subjectId,
                Name = "Patient Assessment",
                DomainId = domainId,
                Domain = domain
            };

            var exam = new Exam
            {
                Id = examId,
                Name = "NGN Initialization Exam",
                ExamProgramId = programId,
                ExamProgram = program
            };

            var clinicalCase = new ClinicalCase
            {
                Id = caseId,
                Title = "Respiratory Assessment",
                Introduction = "A patient presents with respiratory symptoms.",
                ExamProgramId = programId,
                ExamProgram = program,
                DomainId = domainId,
                Domain = domain,
                SubjectId = subjectId,
                Subject = subject,
                Difficulty = Difficulty.Medium,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };

            var firstStage = new ClinicalCaseStage
            {
                Id = firstStageId,
                ClinicalCaseId = caseId,
                ClinicalCase = clinicalCase,
                Title = "Initial Assessment",
                ClinicalInformation = "The patient arrives for assessment.",
                DisplayOrder = 1
            };

            var secondStage = new ClinicalCaseStage
            {
                Id = secondStageId,
                ClinicalCaseId = caseId,
                ClinicalCase = clinicalCase,
                Title = "Follow-up Assessment",
                ClinicalInformation = "Additional clinical information becomes available.",
                DisplayOrder = 2
            };

            var firstQuestion = new Question
            {
                Id = firstQuestionId,
                Type = QuestionType.SingleChoice,
                Stem = "Which assessment should be performed first?",
                ExamProgramId = programId,
                ExamProgram = program,
                DomainId = domainId,
                Domain = domain,
                SubjectId = subjectId,
                Subject = subject,
                Difficulty = Difficulty.Medium
            };

            var secondQuestion = new Question
            {
                Id = secondQuestionId,
                Type = QuestionType.SingleChoice,
                Stem = "Which finding should be assessed next?",
                ExamProgramId = programId,
                ExamProgram = program,
                DomainId = domainId,
                Domain = domain,
                SubjectId = subjectId,
                Subject = subject,
                Difficulty = Difficulty.Medium
            };

            var caseQuestionOne = new ClinicalCaseQuestion
            {
                ClinicalCaseId = caseId,
                ClinicalCase = clinicalCase,
                QuestionId = firstQuestionId,
                Question = firstQuestion,
                ClinicalCaseStageId = firstStageId,
                ClinicalCaseStage = firstStage,
                DisplayOrder = 1,
                ClinicalJudgmentFunction = ClinicalJudgmentFunction.RecognizeCues
            };

            var caseQuestionTwo = new ClinicalCaseQuestion
            {
                ClinicalCaseId = caseId,
                ClinicalCase = clinicalCase,
                QuestionId = secondQuestionId,
                Question = secondQuestion,
                ClinicalCaseStageId = secondStageId,
                ClinicalCaseStage = secondStage,
                DisplayOrder = 2,
                ClinicalJudgmentFunction = ClinicalJudgmentFunction.AnalyzeCues
            };

            db.AddRange(
                program,
                domain,
                subject,
                exam,
                clinicalCase,
                firstStage,
                secondStage,
                firstQuestion,
                secondQuestion,
                caseQuestionOne,
                caseQuestionTwo);

            db.ExamQuestions.AddRange(
                new ExamQuestion
                {
                    ExamId = examId,
                    Exam = exam,
                    QuestionId = firstQuestionId,
                    Question = firstQuestion,
                    DisplayOrder = 1
                },
                new ExamQuestion
                {
                    ExamId = examId,
                    Exam = exam,
                    QuestionId = secondQuestionId,
                    Question = secondQuestion,
                    DisplayOrder = 2
                });

            await db.SaveChangesAsync();
        }

        using var startRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/exams/{examId}/sessions");

        startRequest.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            _studentId);

        startRequest.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        var startResponse = await _client.SendAsync(startRequest);

        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);

        var startBody = await startResponse.Content.ReadFromJsonAsync<StartExamTestResponse>();

        Assert.NotNull(startBody);
        Assert.Equal(examId, startBody.ExamId);
        Assert.Equal("InProgress", startBody.Status);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

            var progress = await db.ExamSessionCaseProgress
                .SingleAsync(p =>
                    p.ExamSessionId == startBody.SessionId &&
                    p.ClinicalCaseId == caseId);

            Assert.Equal(firstStageId, progress.CurrentStageId);
            Assert.False(progress.IsCompleted);
        }

        using var stageRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/exams/sessions/{startBody.SessionId}/cases/{caseId}/current-stage");

        stageRequest.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            _studentId);

        stageRequest.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        var stageResponse = await _client.SendAsync(stageRequest);

        Assert.Equal(HttpStatusCode.OK, stageResponse.StatusCode);

        var stageBody = await stageResponse.Content.ReadFromJsonAsync<CurrentStageTestResponse>();

        Assert.NotNull(stageBody);
        Assert.Equal(firstStageId, stageBody.StageId);
        Assert.Equal("Initial Assessment", stageBody.StageTitle);
        
    }

    public async Task InitializeAsync() { }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private sealed class StartExamTestResponse
    {
        public Guid SessionId { get; set; }
        public Guid ExamId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class CurrentStageTestResponse
    {
        public Guid StageId { get; set; }
        public string StageTitle { get; set; } = string.Empty;
    }
}
