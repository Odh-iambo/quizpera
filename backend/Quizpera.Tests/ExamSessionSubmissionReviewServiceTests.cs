using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamSessionSubmissionReviewServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase(
                $"submission-review-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    [Fact]
    public async Task GetReviewAsync_HidesFutureStageQuestions()
    {
        await using var db = CreateDbContext();

        var fixture = await CreateFixtureAsync(db);

        var service = new ExamSessionSubmissionReviewService(db);

        var result = await service.GetReviewAsync(fixture.Session.Id);

        Assert.NotNull(result);

        var returnedQuestionIds = result.Questions
            .Select(question => question.QuestionId)
            .ToHashSet();

        // Ordinary questions remain accessible.
        Assert.Contains(
            fixture.OrdinaryQuestion.Id,
            returnedQuestionIds);

        // Questions belonging to the current stage remain accessible.
        Assert.Contains(
            fixture.CurrentStageQuestion.Id,
            returnedQuestionIds);

        // Future-stage questions must not be exposed.
        Assert.DoesNotContain(
            fixture.FutureStageQuestion.Id,
            returnedQuestionIds);

        // Summary counts must reflect only accessible questions.
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(2, result.UnansweredQuestions);
        Assert.Equal(0, result.AnsweredQuestions);
        Assert.Equal(0, result.FlaggedQuestions);
        Assert.True(result.HasUnansweredQuestions);
    }

    private sealed record SubmissionReviewFixture(
        ExamSession Session,
        Question OrdinaryQuestion,
        Question CurrentStageQuestion,
        Question FutureStageQuestion);

    private static async Task<SubmissionReviewFixture> CreateFixtureAsync(
        QuizperaDbContext db)
    {
        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Submission Review Test Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Test Domain",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Test Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Submission Review Test Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Exam = exam,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        var clinicalCase = new ClinicalCase
        {
            Id = Guid.NewGuid(),
            Title = "Submission Review Test Case",
            Introduction = "Patient case introduction.",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            Difficulty = Difficulty.Medium,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        var firstStage = new ClinicalCaseStage
        {
            Id = Guid.NewGuid(),
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            Title = "Initial Assessment",
            ClinicalInformation = "Initial patient information.",
            DisplayOrder = 1
        };

        var secondStage = new ClinicalCaseStage
        {
            Id = Guid.NewGuid(),
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            Title = "Further Assessment",
            ClinicalInformation = "Future patient information.",
            DisplayOrder = 2
        };

        Question CreateQuestion(string stem) => new()
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.SingleChoice,
            Stem = stem,
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Medium
        };

        var ordinaryQuestion = CreateQuestion("Ordinary question");
        var currentStageQuestion = CreateQuestion("Current stage question");
        var futureStageQuestion = CreateQuestion("Future stage question");

        db.AddRange(
            program,
            domain,
            subject,
            exam,
            session,
            clinicalCase,
            firstStage,
            secondStage,
            ordinaryQuestion,
            currentStageQuestion,
            futureStageQuestion);

        db.ExamQuestions.AddRange(
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = ordinaryQuestion.Id,
                Question = ordinaryQuestion,
                DisplayOrder = 1
            },
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = currentStageQuestion.Id,
                Question = currentStageQuestion,
                DisplayOrder = 2
            },
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = futureStageQuestion.Id,
                Question = futureStageQuestion,
                DisplayOrder = 3
            });

        db.ClinicalCaseQuestions.AddRange(
            new ClinicalCaseQuestion
            {
                ClinicalCaseId = clinicalCase.Id,
                ClinicalCase = clinicalCase,
                QuestionId = currentStageQuestion.Id,
                Question = currentStageQuestion,
                ClinicalCaseStageId = firstStage.Id,
                ClinicalCaseStage = firstStage,
                DisplayOrder = 1,
                ClinicalJudgmentFunction =
                    ClinicalJudgmentFunction.RecognizeCues
            },
            new ClinicalCaseQuestion
            {
                ClinicalCaseId = clinicalCase.Id,
                ClinicalCase = clinicalCase,
                QuestionId = futureStageQuestion.Id,
                Question = futureStageQuestion,
                ClinicalCaseStageId = secondStage.Id,
                ClinicalCaseStage = secondStage,
                DisplayOrder = 2,
                ClinicalJudgmentFunction =
                    ClinicalJudgmentFunction.AnalyzeCues
            });

        // The session starts at Stage 1.
        db.ExamSessionCaseProgress.Add(new ExamSessionCaseProgress
        {
            ExamSessionId = session.Id,
            ExamSession = session,
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            CurrentStageId = firstStage.Id,
            CurrentStage = firstStage,
            LastUpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return new SubmissionReviewFixture(
            session,
            ordinaryQuestion,
            currentStageQuestion,
            futureStageQuestion);
    }
}