using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Xunit;
using Quizpera.Api.Contracts.Exams;

namespace Quizpera.Tests;

public sealed class ExamSessionQuestionServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"question-service-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    [Fact]
    public async Task GetQuestionsAsync_ReturnsOrdinaryQuestionsAndCurrentStageQuestions()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db, includeProgress: true);

        var service = new ExamSessionQuestionService(db);

        var result = await service.GetQuestionsAsync(fixture.Session.Id);

        Assert.NotNull(result);

        var returnedQuestionIds = result.Questions
            .Select(q => q.QuestionId)
            .ToHashSet();

        Assert.Contains(fixture.OrdinaryQuestion.Id, returnedQuestionIds);
        Assert.Contains(fixture.FirstStageQuestion.Id, returnedQuestionIds);
        Assert.DoesNotContain(fixture.FutureStageQuestion.Id, returnedQuestionIds);
    }

    [Fact]
    public async Task GetQuestionsAsync_HidesCaseQuestionsWhenProgressIsMissing()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db, includeProgress: false);

        var service = new ExamSessionQuestionService(db);

        var result = await service.GetQuestionsAsync(fixture.Session.Id);

        Assert.NotNull(result);

        var returnedQuestionIds = result.Questions
            .Select(q => q.QuestionId)
            .ToHashSet();

        Assert.Contains(fixture.OrdinaryQuestion.Id, returnedQuestionIds);
        Assert.DoesNotContain(fixture.FirstStageQuestion.Id, returnedQuestionIds);
        Assert.DoesNotContain(fixture.FutureStageQuestion.Id, returnedQuestionIds);
    }


    
[Fact]
public async Task GetQuestionsAsync_RestoresSavedAnswerWithoutExposingAnswerKey()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db, includeProgress: true);

    var selectedOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Selected option",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var otherOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Other option",
        DisplayOrder = 2,
        IsCorrect = false
    };

    db.QuestionOptions.AddRange(selectedOption, otherOption);

    var answeredAt = DateTime.UtcNow;

    var savedResponse = new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        ExamSession = fixture.Session,
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        SelectedOptionId = selectedOption.Id,
        SelectedOption = selectedOption,
        IsCorrect = true,
        PointsEarned = 1m,
        MaximumPoints = 1m,
        AnsweredAt = answeredAt
    };

    db.ExamSessionResponses.Add(savedResponse);
    await db.SaveChangesAsync();

    var service = new ExamSessionQuestionService(db);

    var result = await service.GetQuestionsAsync(fixture.Session.Id);

    Assert.NotNull(result);

    var question = Assert.Single(
    result.Questions,
    q => q.QuestionId == fixture.OrdinaryQuestion.Id);

    Assert.NotNull(question.SavedAnswer);
    Assert.Equal(selectedOption.Id, question.SavedAnswer.SelectedOptionId);
    Assert.Contains(
        selectedOption.Id,
        question.SavedAnswer.SelectedOptionIds);
    Assert.Equal(answeredAt, question.SavedAnswer.AnsweredAt);

    // The response DTO must not expose correctness or scoring details.
    var savedAnswerProperties = typeof(ExamSessionSavedAnswerResponse)
        .GetProperties()
        .Select(property => property.Name)
        .ToHashSet();

    Assert.DoesNotContain("IsCorrect", savedAnswerProperties);
    Assert.DoesNotContain("PointsEarned", savedAnswerProperties);
    Assert.DoesNotContain("MaximumPoints", savedAnswerProperties);
}



    [Fact]
public async Task GetQuestionsAsync_HidesQuestionWhenAnotherCaseMappingIsLocked()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db, includeProgress: true);

    // Create a second case with its own two stages.
    var secondCase = new ClinicalCase
    {
        Id = Guid.NewGuid(),
        Title = "Second Test Case",
        Introduction = "Another patient case.",
        ExamProgramId = fixture.Case.ExamProgramId,
        ExamProgram = fixture.Case.ExamProgram,
        DomainId = fixture.Case.DomainId,
        Domain = fixture.Case.Domain,
        Difficulty = Difficulty.Medium,
        IsPublished = true,
        CreatedAt = DateTime.UtcNow
    };

    var secondCaseFirstStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        Title = "Second Case Initial Stage",
        ClinicalInformation = "Initial information.",
        DisplayOrder = 1
    };

    var secondCaseSecondStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        Title = "Second Case Future Stage",
        ClinicalInformation = "Further information.",
        DisplayOrder = 2
    };

    // The same question is unlocked in Case A but locked in Case B.
    var secondCaseMapping = new ClinicalCaseQuestion
    {
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        QuestionId = fixture.FirstStageQuestion.Id,
        Question = fixture.FirstStageQuestion,
        ClinicalCaseStageId = secondCaseSecondStage.Id,
        ClinicalCaseStage = secondCaseSecondStage,
        DisplayOrder = 1,
        ClinicalJudgmentFunction =
            ClinicalJudgmentFunction.AnalyzeCues
    };

    // Case B has progress, but it is still at Stage 1.
    var secondCaseProgress = new ExamSessionCaseProgress
    {
        ExamSessionId = fixture.Session.Id,
        ExamSession = fixture.Session,
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        CurrentStageId = secondCaseFirstStage.Id,
        CurrentStage = secondCaseFirstStage,
        LastUpdatedAt = DateTime.UtcNow
    };

    db.AddRange(
        secondCase,
        secondCaseFirstStage,
        secondCaseSecondStage);

    db.ClinicalCaseQuestions.Add(secondCaseMapping);
    db.ExamSessionCaseProgress.Add(secondCaseProgress);

    await db.SaveChangesAsync();

    var service = new ExamSessionQuestionService(db);

    var result = await service.GetQuestionsAsync(fixture.Session.Id);

    Assert.NotNull(result);

    var returnedQuestionIds = result.Questions
        .Select(q => q.QuestionId)
        .ToHashSet();

    // Ordinary questions remain available.
    Assert.Contains(fixture.OrdinaryQuestion.Id, returnedQuestionIds);

    // The question must be hidden because its Case B mapping is locked,
    // even though its Case A mapping is on the current stage.
    Assert.DoesNotContain(
        fixture.FirstStageQuestion.Id,
        returnedQuestionIds);
}

    [Fact]
    public async Task GetQuestionsAsync_RejectsCompletedSession()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        fixture.Session.Status = ExamSessionStatus.Completed;
        await db.SaveChangesAsync();

        var service = new ExamSessionQuestionService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetQuestionsAsync(fixture.Session.Id));
    }

    [Fact]
    public async Task GetQuestionsAsync_ReturnsNullForMissingSession()
    {
        await using var db = CreateDbContext();

        var service = new ExamSessionQuestionService(db);

        var result = await service.GetQuestionsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private sealed record QuestionFixture(
        ExamSession Session,
        ClinicalCase Case,
        ClinicalCaseStage FirstStage,
        ClinicalCaseStage SecondStage,
        Question OrdinaryQuestion,
        Question FirstStageQuestion,
        Question FutureStageQuestion);

    private static async Task<QuestionFixture> CreateFixtureAsync(
        QuizperaDbContext db,
        bool includeProgress = true)
    {
        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Question Service Test Program"
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
            Name = "Test Exam",
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
            Title = "Test Case",
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
            ClinicalInformation = "Initial information.",
            DisplayOrder = 1
        };

        var secondStage = new ClinicalCaseStage
        {
            Id = Guid.NewGuid(),
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            Title = "Further Assessment",
            ClinicalInformation = "Further information.",
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

        var ordinaryQuestion = CreateQuestion("Ordinary exam question");
        var firstStageQuestion = CreateQuestion("Current stage question");
        var futureStageQuestion = CreateQuestion("Future stage question");

        var mappings = new[]
        {
            new ClinicalCaseQuestion
            {
                ClinicalCaseId = clinicalCase.Id,
                ClinicalCase = clinicalCase,
                QuestionId = firstStageQuestion.Id,
                Question = firstStageQuestion,
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
            }
        };

        var examQuestions = new[]
        {
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
                QuestionId = firstStageQuestion.Id,
                Question = firstStageQuestion,
                DisplayOrder = 2
            },
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = futureStageQuestion.Id,
                Question = futureStageQuestion,
                DisplayOrder = 3
            }
        };

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
            firstStageQuestion,
            futureStageQuestion);

        db.ClinicalCaseQuestions.AddRange(mappings);
        db.ExamQuestions.AddRange(examQuestions);

        if (includeProgress)
        {
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
        }

        await db.SaveChangesAsync();

        return new QuestionFixture(
            session,
            clinicalCase,
            firstStage,
            secondStage,
            ordinaryQuestion,
            firstStageQuestion,
            futureStageQuestion);
    }
}
