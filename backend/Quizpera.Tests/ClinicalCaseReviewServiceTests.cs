using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Xunit;

namespace Quizpera.Tests;

public sealed class ClinicalCaseReviewServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"case-review-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    [Fact]
    public async Task GetReviewAsync_RejectsIncompleteCase()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);
        var service = new ClinicalCaseReviewService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetReviewAsync(
                fixture.Session.Id,
                fixture.Case.Id,
                fixture.UserId));
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsReviewWhenCaseIsCompletedButExamIsInProgress()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var progress = await db.ExamSessionCaseProgress.SingleAsync(
            p => p.ExamSessionId == fixture.Session.Id
                 && p.ClinicalCaseId == fixture.Case.Id);

        progress.IsCompleted = true;

        db.ExamSessionResponses.Add(new ExamSessionResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = fixture.Session.Id,
            QuestionId = fixture.FirstQuestion.Id,
            SelectedOptionId = fixture.FirstOption.Id,
            IsCorrect = false,
            AnsweredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new ClinicalCaseReviewService(db);

        var review = await service.GetReviewAsync(
            fixture.Session.Id,
            fixture.Case.Id,
            fixture.UserId);

        Assert.NotNull(review);
        Assert.Equal(fixture.Session.Id, review.SessionId);
        Assert.Equal("InProgress", review.Status);

        var question = Assert.Single(review.Questions);
        Assert.Equal(fixture.FirstQuestion.Id, question.QuestionId);
        Assert.False(question.IsCorrect);
        Assert.Equal("Initial assessment rationale.", question.Rationale);
        Assert.Equal("Option A", question.SelectedOption?.Text);
        Assert.Equal("Option A", question.CorrectOption?.Text);
    }


    [Fact]
public async Task GetReviewAsync_ReturnsNullWhenCaseQuestionsAreNotInExam()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(db);

    // Mark the case as completed so review eligibility is satisfied.
    var progress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    progress.IsCompleted = true;

    // Remove the case question from the exam.
    // The case remains published and belongs to the same program.
    var examQuestion = await db.ExamQuestions.SingleAsync(
        eq => eq.ExamId == fixture.Session.ExamId
             && eq.QuestionId == fixture.FirstQuestion.Id);

    db.ExamQuestions.Remove(examQuestion);

    await db.SaveChangesAsync();

    var service = new ClinicalCaseReviewService(db);

    var review = await service.GetReviewAsync(
        fixture.Session.Id,
        fixture.Case.Id,
        fixture.UserId);

    Assert.Null(review);
}

    [Fact]
    public async Task GetReviewAsync_ReturnsNullWhenSessionBelongsToAnotherUser()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var progress = await db.ExamSessionCaseProgress.SingleAsync(
            p => p.ExamSessionId == fixture.Session.Id
                 && p.ClinicalCaseId == fixture.Case.Id);

        progress.IsCompleted = true;
        await db.SaveChangesAsync();

        var service = new ClinicalCaseReviewService(db);

        var review = await service.GetReviewAsync(
            fixture.Session.Id,
            fixture.Case.Id,
            "different-user");

        Assert.Null(review);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNullForUnpublishedCase()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        fixture.Case.IsPublished = false;

        var progress = await db.ExamSessionCaseProgress.SingleAsync(
            p => p.ExamSessionId == fixture.Session.Id
                 && p.ClinicalCaseId == fixture.Case.Id);

        progress.IsCompleted = true;
        await db.SaveChangesAsync();

        var service = new ClinicalCaseReviewService(db);

        var review = await service.GetReviewAsync(
            fixture.Session.Id,
            fixture.Case.Id,
            fixture.UserId);

        Assert.Null(review);
    }

    [Fact]
public async Task GetReviewAsync_ReturnsOnlyQuestionsFromRequestedCase()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    // Create a second case in the same exam program.
    var secondCase = new ClinicalCase
    {
        Id = Guid.NewGuid(),
        Title = "Second Clinical Case",
        Introduction = "A separate patient scenario.",
        ExamProgramId = fixture.Case.ExamProgramId,
        ExamProgram = fixture.Case.ExamProgram,
        DomainId = fixture.Case.DomainId,
        Domain = fixture.Case.Domain,
        Difficulty = Difficulty.Medium,
        IsPublished = true,
        CreatedAt = DateTime.UtcNow
    };

    var secondStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        Title = "Second Case Assessment",
        ClinicalInformation = "Information about another patient.",
        DisplayOrder = 1
    };

    var secondQuestion = new Question
    {
        Id = Guid.NewGuid(),
        Type = QuestionType.SingleChoice,
        Stem = "SECOND CASE - Which intervention is appropriate?",
        Rationale = "Second case rationale.",
        ExamProgramId = fixture.FirstQuestion.ExamProgramId,
        ExamProgram = fixture.FirstQuestion.ExamProgram,
        DomainId = fixture.FirstQuestion.DomainId,
        Domain = fixture.FirstQuestion.Domain,
        SubjectId = fixture.FirstQuestion.SubjectId,
        Subject = fixture.FirstQuestion.Subject,
        Difficulty = Difficulty.Medium
    };

    var secondOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = secondQuestion.Id,
        Question = secondQuestion,
        Text = "Second case option",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var secondMapping = new ClinicalCaseQuestion
    {
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        QuestionId = secondQuestion.Id,
        Question = secondQuestion,
        ClinicalCaseStageId = secondStage.Id,
        ClinicalCaseStage = secondStage,
        DisplayOrder = 1,
        ClinicalJudgmentFunction =
            ClinicalJudgmentFunction.RecognizeCues
    };

    db.AddRange(
        secondCase,
        secondStage,
        secondQuestion,
        secondOption,
        secondMapping);

    // Complete only the first case.
    var firstProgress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    firstProgress.IsCompleted = true;

    await db.SaveChangesAsync();

    var service = new ClinicalCaseReviewService(db);

    var review = await service.GetReviewAsync(
        fixture.Session.Id,
        fixture.Case.Id,
        fixture.UserId);

    Assert.NotNull(review);

    var question = Assert.Single(review.Questions);

    Assert.Equal(fixture.FirstQuestion.Id, question.QuestionId);
    Assert.DoesNotContain(
        review.Questions,
        q => q.QuestionId == secondQuestion.Id);
    Assert.DoesNotContain(
        review.Questions,
        q => q.Stem.Contains("SECOND CASE"));
}

    private static async Task<ReviewFixture> CreateFixtureAsync(
        QuizperaDbContext db)
    {
        var userId = $"review-user-{Guid.NewGuid():N}";

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Review Test Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Review Test Domain",
            ExamProgramId = program.Id,
            ExamProgram = program,

        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Review Test Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Review Test Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExamId = exam.Id,
            Exam = exam,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        var clinicalCase = new ClinicalCase
        {
            Id = Guid.NewGuid(),
            Title = "Review Test Case",
            Introduction = "Patient presents for assessment.",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            Difficulty = Difficulty.Medium,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
            
        };

        var stage = new ClinicalCaseStage
        {
            Id = Guid.NewGuid(),
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            Title = "Initial Assessment",
            ClinicalInformation = "Patient information.",
            DisplayOrder = 1
        };

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.SingleChoice,
            Stem = "What should the nurse assess first?",
            Rationale = "Initial assessment rationale.",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Medium
        };

        var option = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            Text = "Option A",
            DisplayOrder = 1,
            IsCorrect = true
        };

        var mapping = new ClinicalCaseQuestion
        {
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            QuestionId = question.Id,
            Question = question,
            ClinicalCaseStageId = stage.Id,
            ClinicalCaseStage = stage,
            DisplayOrder = 1,
            ClinicalJudgmentFunction =
                ClinicalJudgmentFunction.RecognizeCues
        };

        var progress = new ExamSessionCaseProgress
        {
            ExamSessionId = session.Id,
            ExamSession = session,
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            CurrentStageId = stage.Id,
            CurrentStage = stage,
            IsCompleted = false,
            LastUpdatedAt = DateTime.UtcNow
        };

        db.AddRange(
            program,
            domain,
            subject,
            exam,
            session,
            clinicalCase,
            stage,
            question,
            option,
            mapping,
            progress);

        db.ExamQuestions.Add(new ExamQuestion
        {
            ExamId = exam.Id,
            Exam = exam,
            QuestionId = question.Id,
            Question = question,
            DisplayOrder = 1
        });

        await db.SaveChangesAsync();

        return new ReviewFixture(
            userId,
            session,
            clinicalCase,
            question,
            option);
    }

    private sealed record ReviewFixture(
        string UserId,
        ExamSession Session,
        ClinicalCase Case,
        Question FirstQuestion,
        QuestionOption FirstOption);
}
