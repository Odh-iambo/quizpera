using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamSessionNavigationServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"navigation-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }


    
[Fact]
public async Task GetQuestionAsync_RestoresSavedSingleChoiceAnswer()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    var selectedOption = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Selected option",
        DisplayOrder = 1,
        IsCorrect = true
    };

    db.QuestionOptions.Add(selectedOption);

    var answeredAt = DateTime.UtcNow;

    db.ExamSessionResponses.Add(new ExamSessionResponse
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
    });

    await db.SaveChangesAsync();

    var service = new ExamSessionNavigationService(db);

    var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

    Assert.NotNull(result);
    Assert.True(result.IsAnswered);
    Assert.NotNull(result.Question.SavedAnswer);

    Assert.Equal(
        selectedOption.Id,
        result.Question.SavedAnswer.SelectedOptionId);

    Assert.Contains(
        selectedOption.Id,
        result.Question.SavedAnswer.SelectedOptionIds);

    Assert.Equal(
        answeredAt,
        result.Question.SavedAnswer.AnsweredAt);
}


[Fact]
public async Task GetQuestionAsync_RestoresMultipleResponseSelections()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    fixture.OrdinaryQuestion.Type = QuestionType.MultipleResponse;

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Option A",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Option B",
        DisplayOrder = 2,
        IsCorrect = false
    };

    db.QuestionOptions.AddRange(optionA, optionB);

    var answeredAt = DateTime.UtcNow;

    var response = new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        ExamSession = fixture.Session,
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        IsCorrect = false,
        PointsEarned = 0m,
        MaximumPoints = 1m,
        AnsweredAt = answeredAt
    };

    response.SelectedOptions.Add(new ExamSessionResponseOption
    {
        ExamSessionResponse = response,
        ExamSessionResponseId = response.Id,
        QuestionOption = optionA,
        QuestionOptionId = optionA.Id
    });

    response.SelectedOptions.Add(new ExamSessionResponseOption
    {
        ExamSessionResponse = response,
        ExamSessionResponseId = response.Id,
        QuestionOption = optionB,
        QuestionOptionId = optionB.Id
    });

    db.ExamSessionResponses.Add(response);
    await db.SaveChangesAsync();

    var service = new ExamSessionNavigationService(db);

    var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

    Assert.NotNull(result);
    Assert.True(result.IsAnswered);
    Assert.NotNull(result.Question.SavedAnswer);


Assert.Equal(
    new[] { optionA.Id, optionB.Id }.OrderBy(id => id),
    result.Question.SavedAnswer.SelectedOptionIds.OrderBy(id => id));

      

    Assert.All(
        result.Question.SavedAnswer.SelectedOrders,
        order => Assert.Null(order));
}



[Fact]
public async Task GetQuestionAsync_RestoresOrderedResponseSequence()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    fixture.OrdinaryQuestion.Type = QuestionType.OrderedResponse;

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "First action",
        DisplayOrder = 1,
        CorrectOrder = 1
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Second action",
        DisplayOrder = 2,
        CorrectOrder = 2
    };

    var optionC = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        Text = "Third action",
        DisplayOrder = 3,
        CorrectOrder = 3
    };

    db.QuestionOptions.AddRange(optionA, optionB, optionC);

    var answeredAt = DateTime.UtcNow;

    var response = new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        ExamSession = fixture.Session,
        QuestionId = fixture.OrdinaryQuestion.Id,
        Question = fixture.OrdinaryQuestion,
        IsCorrect = true,
        PointsEarned = 1m,
        MaximumPoints = 1m,
        AnsweredAt = answeredAt
    };

    var submittedOptions = new[]
    {
        (Option: optionC, Order: 1),
        (Option: optionA, Order: 2),
        (Option: optionB, Order: 3)
    };

    foreach (var item in submittedOptions)
    {
        response.SelectedOptions.Add(new ExamSessionResponseOption
        {
            ExamSessionResponse = response,
            ExamSessionResponseId = response.Id,
            QuestionOption = item.Option,
            QuestionOptionId = item.Option.Id,
            SelectedOrder = item.Order
        });
    }

    db.ExamSessionResponses.Add(response);
    await db.SaveChangesAsync();

    var service = new ExamSessionNavigationService(db);

    var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

    Assert.NotNull(result);
    Assert.True(result.IsAnswered);
    Assert.NotNull(result.Question.SavedAnswer);

    Assert.Equal(
        new[] { optionC.Id, optionA.Id, optionB.Id },
        result.Question.SavedAnswer.SelectedOptionIds);

    Assert.Equal(
        new int?[] { 1, 2, 3 },
        result.Question.SavedAnswer.SelectedOrders);

    Assert.Equal(answeredAt, result.Question.SavedAnswer.AnsweredAt);
}


    [Fact]
    public async Task GetQuestionAsync_ReturnsCurrentQuestionAndHidesFutureStage()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var service = new ExamSessionNavigationService(db);

        var ordinary = await service.GetQuestionAsync(fixture.Session.Id, 1);

        Assert.NotNull(ordinary);
        Assert.Equal(fixture.OrdinaryQuestion.Id, ordinary.Question.QuestionId);
        Assert.Equal(2, ordinary.TotalQuestions);

        var currentStage = await service.GetQuestionAsync(fixture.Session.Id, 2);

        Assert.NotNull(currentStage);
        Assert.Equal(
            fixture.FirstStageQuestion.Id,
            currentStage.Question.QuestionId);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetQuestionAsync(fixture.Session.Id, 3));
    }

    [Fact]
    public async Task GetQuestionAsync_HidesCaseQuestionsWhenProgressIsMissing()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db, includeProgress: false);

        var service = new ExamSessionNavigationService(db);

        var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(fixture.OrdinaryQuestion.Id, result.Question.QuestionId);
        Assert.Equal(1, result.TotalQuestions);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetQuestionAsync(fixture.Session.Id, 2));
    }

    [Fact]
    public async Task GetQuestionAsync_RejectsPositionBelowOne()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var service = new ExamSessionNavigationService(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetQuestionAsync(fixture.Session.Id, 0));
    }

    [Fact]
    public async Task GetQuestionAsync_RejectsCompletedSession()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        fixture.Session.Status = ExamSessionStatus.Completed;
        await db.SaveChangesAsync();

        var service = new ExamSessionNavigationService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetQuestionAsync(fixture.Session.Id, 1));
    }

    [Fact]
    public async Task GetQuestionAsync_ReturnsNullForMissingSession()
    {
        await using var db = CreateDbContext();

        var service = new ExamSessionNavigationService(db);

        var result = await service.GetQuestionAsync(Guid.NewGuid(), 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestionAsync_DoesNotReturnAnswerKeyOrRationale()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        fixture.OrdinaryQuestion.Rationale =
            "This explanation must not be returned during the exam.";

        db.QuestionOptions.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = fixture.OrdinaryQuestion.Id,
            Question = fixture.OrdinaryQuestion,
            Text = "Correct option",
            DisplayOrder = 1,
            IsCorrect = true
        });

        await db.SaveChangesAsync();

        var service = new ExamSessionNavigationService(db);

        var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(
            fixture.OrdinaryQuestion.Id,
            result.Question.QuestionId);

        // Verify the response DTO doesn't expose answer-key properties.
        var optionType = Assert.Single(result.Question.Options).GetType();

        Assert.Null(optionType.GetProperty("IsCorrect"));
        Assert.Null(optionType.GetProperty("Rationale"));

        var questionType = result.Question.GetType();

        Assert.Null(questionType.GetProperty("Rationale"));
        Assert.Null(questionType.GetProperty("CorrectAnswer"));
    }

    [Fact]
    public async Task GetQuestionAsync_HidesQuestionWhenAnotherCaseMappingIsLocked()
    {
        await using var db = CreateDbContext();
        var fixture = await CreateFixtureAsync(db);

        var secondCase = new ClinicalCase
        {
            Id = Guid.NewGuid(),
            Title = "Second Navigation Test Case",
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

        // This question is in Case A's current stage but Case B's future stage.
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

        var service = new ExamSessionNavigationService(db);

        // Only the ordinary question remains accessible.
        var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(
            fixture.OrdinaryQuestion.Id,
            result.Question.QuestionId);
        Assert.Equal(1, result.TotalQuestions);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetQuestionAsync(fixture.Session.Id, 2));
    }
[Fact]
public async Task GetQuestionAsync_RestoresSavedNumericEntryAnswerWithoutExposingAnswerKey()
{
    await using var db = CreateDbContext();
    var fixture = await CreateFixtureAsync(db);

    var question = fixture.OrdinaryQuestion;
    question.Type = QuestionType.NumericEntry;
    question.NumericCorrectAnswer = 13m;
    question.NumericTolerance = 0.2m;
    question.Rationale = "This rationale must not be exposed during navigation.";

    var answeredAt = DateTime.UtcNow;

    db.ExamSessionResponses.Add(new ExamSessionResponse
    {
        Id = Guid.NewGuid(),
        ExamSessionId = fixture.Session.Id,
        ExamSession = fixture.Session,
        QuestionId = question.Id,
        Question = question,
        NumericAnswer = 12.8m,
        IsCorrect = true,
        PointsEarned = 1m,
        MaximumPoints = 1m,
        AnsweredAt = answeredAt
    });

    await db.SaveChangesAsync();

    var service = new ExamSessionNavigationService(db);

    var result = await service.GetQuestionAsync(fixture.Session.Id, 1);

    Assert.NotNull(result);
    Assert.True(result.IsAnswered);
    Assert.NotNull(result.Question.SavedAnswer);

    Assert.Equal(
        12.8m,
        result.Question.SavedAnswer.NumericAnswer);

    Assert.Equal(
        answeredAt,
        result.Question.SavedAnswer.AnsweredAt);

    var questionResponseType = result.Question.GetType();

    Assert.Null(questionResponseType.GetProperty("NumericCorrectAnswer"));
    Assert.Null(questionResponseType.GetProperty("NumericTolerance"));
    Assert.Null(questionResponseType.GetProperty("Rationale"));

    var savedAnswerType = result.Question.SavedAnswer.GetType();

    Assert.Null(savedAnswerType.GetProperty("IsCorrect"));
    Assert.Null(savedAnswerType.GetProperty("PointsEarned"));
    Assert.Null(savedAnswerType.GetProperty("MaximumPoints"));
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
            Name = "Navigation Test Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Navigation Test Domain",
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Navigation Test Subject",
            DomainId = domain.Id,
            Domain = domain
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Navigation Test Exam",
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
            Title = "Navigation Test Case",
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