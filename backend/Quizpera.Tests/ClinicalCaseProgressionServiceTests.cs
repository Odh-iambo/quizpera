using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using SQLitePCL;
using Xunit;

namespace Quizpera.Tests;

public sealed class ClinicalCaseProgressionServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"ngn-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    [Fact]
    public async Task InitializeSessionCasesAsync_RejectsSessionFromDifferentExam()
    {
        await using var db = CreateDbContext();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "NGN Test Program"
        };

        var examA = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Exam A",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var examB = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Exam B",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = examA.Id,
            Exam = examA,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.AddRange(program, examA, examB, session);
        await db.SaveChangesAsync();

        var service = new ClinicalCaseProgressionService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitializeSessionCasesAsync(session.Id, examB.Id));

        Assert.Contains("does not belong", exception.Message);
    }

    [Theory]
    [InlineData(ExamSessionStatus.Completed)]
    [InlineData(ExamSessionStatus.Abandoned)]
    public async Task InitializeSessionCasesAsync_RejectsInactiveSession(
        ExamSessionStatus status)
    {
        await using var db = CreateDbContext();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "NGN Test Program"
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "NGN Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Exam = exam,
            Status = status,
            StartedAt = DateTime.UtcNow
        };

        db.AddRange(program, exam, session);
        await db.SaveChangesAsync();

        var service = new ClinicalCaseProgressionService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitializeSessionCasesAsync(session.Id, exam.Id));
    }

    [Fact]
    public async Task InitializeSessionCasesAsync_RejectsMissingSession()
    {
        await using var db = CreateDbContext();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "NGN Test Program"
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "NGN Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        db.AddRange(program, exam);
        await db.SaveChangesAsync();

        var service = new ClinicalCaseProgressionService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InitializeSessionCasesAsync(
                Guid.NewGuid(),
                exam.Id));
    }


        [Fact]
    public async Task CanAccessQuestionAsync_AllowsOrdinaryQuestion()
    {
        await using var db = CreateDbContext();
        var service = new ClinicalCaseProgressionService(db);

        var result = await service.CanAccessQuestionAsync(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessQuestionAsync_DeniesCaseQuestionWithoutProgress()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db, includeProgress: false);
        var service = new ClinicalCaseProgressionService(db);

        var result = await service.CanAccessQuestionAsync(
            fixture.Session.Id,
            fixture.FirstQuestion.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task AdvanceToNextStageAsync_RejectsUnansweredQuestions()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);
        var service = new ClinicalCaseProgressionService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AdvanceToNextStageAsync(
                fixture.Session.Id,
                fixture.Case.Id));

        Assert.Contains("All questions", exception.Message);
    }


    [Fact]
public async Task AdvanceToNextStageAsync_RejectsWhenOneOfMultipleStageQuestionsIsUnanswered()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        putSecondQuestionInFirstStage: true);

    var service = new ClinicalCaseProgressionService(db);

    // Answer only the first of the two questions in the current stage.
    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = fixture.FirstQuestion.Id,
        SelectedOptionId = fixture.FirstOption.Id,
        IsCorrect = true,
        AnsweredAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id));

    var progress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    Assert.Equal(fixture.FirstStage.Id, progress.CurrentStageId);
}


[Fact]
public async Task AdvanceToNextStageAsync_UnrelatedResponseDoesNotSatisfyRequiredQuestion()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        putSecondQuestionInFirstStage: true);

    var service = new ClinicalCaseProgressionService(db);

    // Answer the first required question.
    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = fixture.FirstQuestion.Id,
        SelectedOptionId = fixture.FirstOption.Id,
        IsCorrect = true,
        AnsweredAt = DateTime.UtcNow
    });

    // Create an ordinary exam question that is not part of this clinical case.
    var unrelatedQuestion = CreateQuestion(
        fixture.Case.ExamProgram!,
        fixture.Case.Domain!,
        fixture.Case.Domain!.Subjects.First(),
        "Unrelated ordinary exam question");

    var unrelatedOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = unrelatedQuestion.Id,
        Question = unrelatedQuestion,
        Text = "Unrelated option",
        DisplayOrder = 1,
        IsCorrect = true
    };

    db.Questions.Add(unrelatedQuestion);
    db.QuestionOptions.Add(unrelatedOption);

    db.ExamQuestions.Add(new ExamQuestion
    {
        ExamId = fixture.Session.ExamId,
        Exam = fixture.Session.Exam,
        QuestionId = unrelatedQuestion.Id,
        Question = unrelatedQuestion,
        DisplayOrder = 3
    });

    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = unrelatedQuestion.Id,
        SelectedOptionId = unrelatedOption.Id,
        IsCorrect = true,
        AnsweredAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id));

    var progress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    Assert.Equal(fixture.FirstStage.Id, progress.CurrentStageId);
}


 [Theory]
[InlineData(ExamSessionStatus.Completed)]
[InlineData(ExamSessionStatus.Abandoned)]
public async Task AdvanceToNextStageAsync_RejectsInactiveSession(
    ExamSessionStatus status)
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(db);

    fixture.Session.Status = status;
    await db.SaveChangesAsync();

    var service = new ClinicalCaseProgressionService(db);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id));
}

    [Fact]
    public async Task AdvanceToNextStageAsync_AdvancesAfterAllRequiredQuestionsAnswered()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);
        var service = new ClinicalCaseProgressionService(db);

        db.ExamSessionResponses.Add(
            new ExamSessionResponse
            {
                Id = Guid.NewGuid(),
                ExamSessionId = fixture.Session.Id,
                QuestionId = fixture.FirstQuestion.Id,
                SelectedOptionId = fixture.FirstOption.Id,
                IsCorrect = true,
                AnsweredAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var advanced = await service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id);

        Assert.Equal(ClinicalCaseAdvanceResult.Advanced, advanced);

        var progress = await db.ExamSessionCaseProgress
            .SingleAsync(p =>
                p.ExamSessionId == fixture.Session.Id &&
                p.ClinicalCaseId == fixture.Case.Id);

        Assert.Equal(fixture.SecondStage.Id, progress.CurrentStageId);
    }

    [Fact]
    public async Task AdvanceToNextStageAsync_AdvancesWhenRequiredQuestionAnsweredIncorrectly()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var service = new ClinicalCaseProgressionService(db);

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

        var advanced = await service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id);

        Assert.Equal(ClinicalCaseAdvanceResult.Advanced, advanced);

        var progress = await db.ExamSessionCaseProgress
            .SingleAsync(p =>
                p.ExamSessionId == fixture.Session.Id &&
                p.ClinicalCaseId == fixture.Case.Id);

        Assert.Equal(
            fixture.SecondStage.Id,
            progress.CurrentStageId);
    }

    

[Fact]
public async Task InitializeSessionCasesAsync_PreservesProgressWhenCalledAgain()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    var service = new ClinicalCaseProgressionService(db);

    // Answer the first-stage question.
    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = fixture.FirstQuestion.Id,
        SelectedOptionId = fixture.FirstOption.Id,
        IsCorrect = true,
        AnsweredAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    // Advance to the second stage.
    var advanced = await service.AdvanceToNextStageAsync(
        fixture.Session.Id,
        fixture.Case.Id);

    Assert.Equal(ClinicalCaseAdvanceResult.Advanced, advanced);

    // Initialize the same session again.
    await service.InitializeSessionCasesAsync(
        fixture.Session.Id,
        fixture.Session.ExamId);

    // Confirm progress remains on the second stage.
    var progress = await db.ExamSessionCaseProgress
        .SingleAsync(p =>
            p.ExamSessionId == fixture.Session.Id &&
            p.ClinicalCaseId == fixture.Case.Id);

    Assert.Equal(
        fixture.SecondStage.Id,
        progress.CurrentStageId);

    // Confirm no duplicate progress record was created.
    var progressCount = await db.ExamSessionCaseProgress
        .CountAsync(p =>
            p.ExamSessionId == fixture.Session.Id &&
            p.ClinicalCaseId == fixture.Case.Id);

    Assert.Equal(1, progressCount);
}

        [Fact]
    public async Task CanAccessQuestionAsync_DeniesQuestionInFutureStage()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);
        var service = new ClinicalCaseProgressionService(db);

        // The session is still on the first stage.
        var result = await service.CanAccessQuestionAsync(
            fixture.Session.Id,
            fixture.SecondQuestion.Id);

        Assert.False(result);
    }


    [Fact]
public async Task AdvanceToNextStageAsync_DoesNotChangeAnotherCasesProgress()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    // Arrange: create a second case in the same exam program.
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

    var secondCaseFirstStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        Title = "Second Case Initial Assessment",
        ClinicalInformation = "Initial information for the second case.",
        DisplayOrder = 1
    };

    var secondCaseSecondStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        Title = "Second Case Further Assessment",
        ClinicalInformation = "Further information for the second case.",
        DisplayOrder = 2
    };

    var secondCaseQuestion = new Question
    {
        Id = Guid.NewGuid(),
        Type = QuestionType.SingleChoice,
        Stem = "What should be assessed in the second case?",
        ExamProgramId = fixture.FirstQuestion.ExamProgramId,
        ExamProgram = fixture.FirstQuestion.ExamProgram,
        DomainId = fixture.FirstQuestion.DomainId,
        Domain = fixture.FirstQuestion.Domain,
        SubjectId = fixture.FirstQuestion.SubjectId,
        Subject = fixture.FirstQuestion.Subject,
        Difficulty = Difficulty.Medium
    };

    var secondCaseOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = secondCaseQuestion.Id,
        Question = secondCaseQuestion,
        Text = "Assess the patient",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var secondCaseMapping = new ClinicalCaseQuestion
    {
        ClinicalCaseId = secondCase.Id,
        ClinicalCase = secondCase,
        QuestionId = secondCaseQuestion.Id,
        Question = secondCaseQuestion,
        ClinicalCaseStageId = secondCaseFirstStage.Id,
        ClinicalCaseStage = secondCaseFirstStage,
        DisplayOrder = 1,
        ClinicalJudgmentFunction =
            ClinicalJudgmentFunction.RecognizeCues
    };

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
        secondCaseSecondStage,
        secondCaseQuestion,
        secondCaseOption,
        secondCaseMapping,
        secondCaseProgress);

    await db.SaveChangesAsync();

    // Answer Case A's required first-stage question.
    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = fixture.FirstQuestion.Id,
        SelectedOptionId = fixture.FirstOption.Id,
        IsCorrect = true,
        AnsweredAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    var service = new ClinicalCaseProgressionService(db);

    // Act: advance only Case A.
    var advanced = await service.AdvanceToNextStageAsync(
        fixture.Session.Id,
        fixture.Case.Id);

    // Assert: Case A advances successfully.
    Assert.Equal(ClinicalCaseAdvanceResult.Advanced, advanced);

    var firstCaseProgress = await db.ExamSessionCaseProgress
        .SingleAsync(p =>
            p.ExamSessionId == fixture.Session.Id &&
            p.ClinicalCaseId == fixture.Case.Id);
            

    Assert.Equal(
        fixture.SecondStage.Id,
        firstCaseProgress.CurrentStageId);


    // Assert: Case B remains on its original stage.
    var secondCaseProgressAfterAdvance =
        await db.ExamSessionCaseProgress.SingleAsync(p =>
            p.ExamSessionId == fixture.Session.Id &&
            p.ClinicalCaseId == secondCase.Id);

    Assert.Equal(
        secondCaseFirstStage.Id,
        secondCaseProgressAfterAdvance.CurrentStageId);
}

    [Fact]
    public async Task AdvanceToNextStageAsync_ReturnsCaseCompletedAtFinalStage()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);
        var service = new ClinicalCaseProgressionService(db);

        var progress = await db.ExamSessionCaseProgress.SingleAsync(
            p => p.ExamSessionId == fixture.Session.Id
                 && p.ClinicalCaseId == fixture.Case.Id);

        // Move the session to the final stage.
        progress.CurrentStageId = fixture.SecondStage.Id;
        await db.SaveChangesAsync();

        // Answer the final stage's required question.
        db.ExamSessionResponses.Add(new ExamSessionResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = fixture.Session.Id,
            QuestionId = fixture.SecondQuestion.Id,
            SelectedOptionId = fixture.SecondOption.Id,
            IsCorrect = true,
            AnsweredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var advanced = await service.AdvanceToNextStageAsync(
            fixture.Session.Id,
            fixture.Case.Id);

        Assert.Equal(ClinicalCaseAdvanceResult.CaseCompleted, advanced);

        // The final stage remains current.
        var updatedProgress = await db.ExamSessionCaseProgress.SingleAsync(
            p => p.ExamSessionId == fixture.Session.Id
                 && p.ClinicalCaseId == fixture.Case.Id);

        Assert.Equal(fixture.SecondStage.Id, updatedProgress.CurrentStageId);
    }

    [Fact]
public async Task AdvanceToNextStageAsync_MarksCaseCompletedAtFinalStage()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);
    var service = new ClinicalCaseProgressionService(db);

    var progress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    progress.CurrentStageId = fixture.SecondStage.Id;
    await db.SaveChangesAsync();

    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        QuestionId = fixture.SecondQuestion.Id,
        SelectedOptionId = fixture.SecondOption.Id,
        IsCorrect = false,
        AnsweredAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    var advanced = await service.AdvanceToNextStageAsync(
        fixture.Session.Id,
        fixture.Case.Id);

    var updatedProgress = await db.ExamSessionCaseProgress.SingleAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    Assert.Equal(ClinicalCaseAdvanceResult.CaseCompleted, advanced);
    Assert.True(updatedProgress.IsCompleted);
    Assert.Equal(fixture.SecondStage.Id, updatedProgress.CurrentStageId);
}

    [Fact]
public async Task InitializeSessionCasesAsync_IgnoresUnrelatedPublishedCases()
{
    await using var db = CreateDbContext();

    // Arrange: create the normal exam and clinical case fixture.
    var fixture = await CreateFixtureAsync(db);

    // Create another published case in the same exam program.
    // Its question will NOT be included in this exam.
    var unrelatedCase = new ClinicalCase
    {
        Id = Guid.NewGuid(),
        Title = "Unrelated Published Case",
        Introduction = "A separate patient scenario.",
        ExamProgramId = fixture.Case.ExamProgramId,
        ExamProgram = fixture.Case.ExamProgram,
        DomainId = fixture.Case.DomainId,
        Domain = fixture.Case.Domain,
        Difficulty = Difficulty.Medium,
        IsPublished = true,
        CreatedAt = DateTime.UtcNow
    };

    var unrelatedStage = new ClinicalCaseStage
    {
        Id = Guid.NewGuid(),
        ClinicalCaseId = unrelatedCase.Id,
        ClinicalCase = unrelatedCase,
        Title = "Initial Assessment",
        ClinicalInformation = "Information for an unrelated patient.",
        DisplayOrder = 1
    };

    var unrelatedQuestion = CreateQuestion(
        fixture.FirstQuestion.ExamProgram!,
        fixture.FirstQuestion.Domain!,
        fixture.FirstQuestion.Subject!,
        "Question belonging only to the unrelated case");

    var unrelatedMapping = new ClinicalCaseQuestion
    {
        ClinicalCaseId = unrelatedCase.Id,
        ClinicalCase = unrelatedCase,
        QuestionId = unrelatedQuestion.Id,
        Question = unrelatedQuestion,
        ClinicalCaseStageId = unrelatedStage.Id,
        ClinicalCaseStage = unrelatedStage,
        DisplayOrder = 1,
        ClinicalJudgmentFunction =
            ClinicalJudgmentFunction.RecognizeCues
    };

    db.AddRange(
        unrelatedCase,
        unrelatedStage,
        unrelatedQuestion,
        unrelatedMapping);

    await db.SaveChangesAsync();

    var service = new ClinicalCaseProgressionService(db);

    // Act: initialize the session.
    await service.InitializeSessionCasesAsync(
        fixture.Session.Id,
        fixture.Session.ExamId);

    // Assert: the valid case remains initialized.
    var initializedCaseIds = await db.ExamSessionCaseProgress
        .Where(p => p.ExamSessionId == fixture.Session.Id)
        .Select(p => p.ClinicalCaseId)
        .ToListAsync();

    Assert.Contains(fixture.Case.Id, initializedCaseIds);

    // The unrelated case must not be initialized for this exam.
    Assert.DoesNotContain(unrelatedCase.Id, initializedCaseIds);
}

    [Fact]
public async Task GetCurrentStageAsync_ReturnsNullWhenCaseQuestionsAreNotInExam()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);
    var service = new ClinicalCaseProgressionService(db);

    // Confirm the case has progress for this session.
    var progressExists = await db.ExamSessionCaseProgress.AnyAsync(
        p => p.ExamSessionId == fixture.Session.Id
             && p.ClinicalCaseId == fixture.Case.Id);

    Assert.True(progressExists);

    // Remove both questions belonging to this case from the exam.
    var caseQuestionIds = await db.ClinicalCaseQuestions
        .Where(cq => cq.ClinicalCaseId == fixture.Case.Id)
        .Select(cq => cq.QuestionId)
        .ToListAsync();

    var examQuestions = await db.ExamQuestions
        .Where(eq =>
            eq.ExamId == fixture.Session.ExamId &&
            caseQuestionIds.Contains(eq.QuestionId))
        .ToListAsync();

    db.ExamQuestions.RemoveRange(examQuestions);
    await db.SaveChangesAsync();

    // A case with progress but no questions assigned to this exam
    // must not expose its stage information.
    var result = await service.GetCurrentStageAsync(
        fixture.Session.Id,
        fixture.Case.Id);

    Assert.Null(result);
}

    private sealed record ProgressionFixture(
        ExamSession Session,
        ClinicalCase Case,
        ClinicalCaseStage FirstStage,
        ClinicalCaseStage SecondStage,
        Question FirstQuestion,
        Question SecondQuestion,
        QuestionOption FirstOption,
        QuestionOption SecondOption);
        

    private static async Task<ProgressionFixture> CreateFixtureAsync(
        QuizperaDbContext db,
        bool includeProgress = true,
        bool putSecondQuestionInFirstStage = false)
    {
        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "NGN Test Program"
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
            Name = "NGN Test Exam",
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
            Title = "Test Clinical Case",
            Introduction = "Patient presents for assessment.",
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
            Title = "Follow-up",
            ClinicalInformation = "Updated patient information.",
            DisplayOrder = 2
        };

        var firstQuestion = CreateQuestion(
            program, domain, subject, "Initial assessment question");

        var secondQuestion = CreateQuestion(
            program, domain, subject, "Follow-up question");

        var firstOption = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = firstQuestion.Id,
            Question = firstQuestion,
            Text = "Option A",
            DisplayOrder = 1,
            IsCorrect = true
        };

        var secondOption = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = secondQuestion.Id,
            Question = secondQuestion,
            Text = "Option B",
            DisplayOrder = 1,
            IsCorrect = true
        };

        var firstMapping = new ClinicalCaseQuestion
        {
            ClinicalCaseId = clinicalCase.Id,
            ClinicalCase = clinicalCase,
            QuestionId = firstQuestion.Id,
            Question = firstQuestion,
            ClinicalCaseStageId = firstStage.Id,
            ClinicalCaseStage = firstStage,
            DisplayOrder = 1,
            ClinicalJudgmentFunction =
                ClinicalJudgmentFunction.RecognizeCues
        };

        var secondMapping = new ClinicalCaseQuestion
{
    ClinicalCaseId = clinicalCase.Id,
    ClinicalCase = clinicalCase,
    QuestionId = secondQuestion.Id,
    Question = secondQuestion,
    ClinicalCaseStageId = putSecondQuestionInFirstStage
        ? firstStage.Id
        : secondStage.Id,
    ClinicalCaseStage = putSecondQuestionInFirstStage
        ? firstStage
        : secondStage,
    DisplayOrder = 2,
    ClinicalJudgmentFunction =
        ClinicalJudgmentFunction.AnalyzeCues
};

        var examQuestions = new[]
        {
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = firstQuestion.Id,
                Question = firstQuestion,
                DisplayOrder = 2
            },
            new ExamQuestion
            {
                ExamId = exam.Id,
                Exam = exam,
                QuestionId = secondQuestion.Id,
                Question = secondQuestion,
                DisplayOrder = 2
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
            firstQuestion,
            secondQuestion,
            firstOption,
            secondOption,
            firstMapping,
            secondMapping);
        db.ExamQuestions.AddRange(examQuestions);

        if (includeProgress)
        {
            db.ExamSessionCaseProgress.Add(
                new ExamSessionCaseProgress
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

        return new ProgressionFixture(
            session,
            clinicalCase,
            firstStage,
            secondStage,
            firstQuestion,
            secondQuestion,
            firstOption,
            secondOption);
    }

    private static Question CreateQuestion(
        ExamProgram program,
        Quizpera.Api.Domain.Questions.Domain domain,
        Subject subject,
        string stem)
    {
        return new Question
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
    }



    
}
