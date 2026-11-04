using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services.Evaluation;

namespace Quizpera.Tests;

public class BowTieEvaluatorTests
{
    private static QuizperaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"bowtie-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    private static Question CreateQuestion(
        QuestionScoringMethod scoringMethod = QuestionScoringMethod.ZeroOne,
        decimal maximumPoints = 5m)
    {
        return new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.BowTie,
            Stem = "Test Bow-Tie question",
            ExamProgramId = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            Difficulty = Difficulty.Medium,
            MaximumPoints = maximumPoints,
            ScoringMethod = scoringMethod
        };
    }

    private static BowTieOption CreateOption(
        Guid questionId,
        BowTieOptionCategory category,
        bool isCorrect,
        int displayOrder)
    {
        return new BowTieOption
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Category = category,
            Text = $"Option {Guid.NewGuid():N}",
            IsCorrect = isCorrect,
            DisplayOrder = displayOrder
        };
    }

   private sealed record BowTieSeed(
    Question Question,
    BowTieOption Condition,
    BowTieOption Action1,
    BowTieOption Action2,
    BowTieOption Parameter1,
    BowTieOption Parameter2,
    BowTieOption DistractorCondition,
    BowTieOption DistractorAction1,
    BowTieOption DistractorAction2,
    BowTieOption DistractorParameter1,
    BowTieOption DistractorParameter2);
    

private static async Task<BowTieSeed> SeedQuestionAsync(
    QuizperaDbContext db,
    QuestionScoringMethod scoringMethod =
        QuestionScoringMethod.ZeroOne,
    decimal maximumPoints = 5m)
{
    var question = CreateQuestion(scoringMethod, maximumPoints);

    var condition = CreateOption(
        question.Id, BowTieOptionCategory.Condition, true, 1);

    var action1 = CreateOption(
        question.Id, BowTieOptionCategory.Action, true, 1);

    var action2 = CreateOption(
        question.Id, BowTieOptionCategory.Action, true, 2);

    var parameter1 = CreateOption(
        question.Id, BowTieOptionCategory.Parameter, true, 1);

    var parameter2 = CreateOption(
        question.Id, BowTieOptionCategory.Parameter, true, 2);

    var distractorCondition = CreateOption(
        question.Id, BowTieOptionCategory.Condition, false, 2);

    var distractorAction1 = CreateOption(
        question.Id, BowTieOptionCategory.Action, false, 3);

    var distractorAction2 = CreateOption(
        question.Id, BowTieOptionCategory.Action, false, 4);

    var distractorParameter1 = CreateOption(
        question.Id, BowTieOptionCategory.Parameter, false, 3);

    var distractorParameter2 = CreateOption(
        question.Id, BowTieOptionCategory.Parameter, false, 4);

    db.Questions.Add(question);

    db.BowTieOptions.AddRange(
        condition,
        action1,
        action2,
        parameter1,
        parameter2,
        distractorCondition,
        distractorAction1,
        distractorAction2,
        distractorParameter1,
        distractorParameter2);

    await db.SaveChangesAsync();

    return new BowTieSeed(
        question,
        condition,
        action1,
        action2,
        parameter1,
        parameter2,
        distractorCondition,
        distractorAction1,
        distractorAction2,
        distractorParameter1,
        distractorParameter2);
}

    private static List<Guid> GetSelectionsWithCorrectCount(
    BowTieSeed seed,
    int correctSelections)
{
    if (correctSelections < 0 || correctSelections > 5)
    {
        throw new ArgumentOutOfRangeException(
            nameof(correctSelections));
    }

    var selectedIds = new List<Guid>();

    var remaining = correctSelections;

    // Exactly one Condition.
    if (remaining > 0)
    {
        selectedIds.Add(seed.Condition.Id);
        remaining--;
    }
    else
    {
        selectedIds.Add(seed.DistractorCondition.Id);
    }

    // Exactly two Actions.
    var correctActions = Math.Min(2, remaining);

    selectedIds.Add(
        correctActions >= 1
            ? seed.Action1.Id
            : seed.DistractorAction1.Id);

    selectedIds.Add(
        correctActions >= 2
            ? seed.Action2.Id
            : seed.DistractorAction2.Id);

    remaining -= correctActions;

    // Exactly two Parameters.
    var correctParameters = Math.Min(2, remaining);

    selectedIds.Add(
        correctParameters >= 1
            ? seed.Parameter1.Id
            : seed.DistractorParameter1.Id);

    selectedIds.Add(
        correctParameters >= 2
            ? seed.Parameter2.Id
            : seed.DistractorParameter2.Id);

    return selectedIds;
}
    private static List<Guid> GetCorrectSelections(BowTieSeed seed)
    {
        return
        [
            seed.Condition.Id,
            seed.Action1.Id,
            seed.Action2.Id,
            seed.Parameter1.Id,
            seed.Parameter2.Id
        ];
    }

    

    // ==========================================
    // CORRECT ANSWERS
    // ==========================================

    [Fact]
    public async Task EvaluateAsync_AllCorrectSelections_ReturnsFullScore()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = GetCorrectSelections(seed)
            });

        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.Equal(5m, result.PointsEarned);
        Assert.Equal(5m, result.MaximumPoints);
        Assert.Equal(5, result.BowTieOptionIds.Count);
    }

    // ==========================================
    // ZERO-ONE SCORING
    // ==========================================
[Theory]
[InlineData(5, 5)]
[InlineData(4, 4)]
[InlineData(3, 3)]
[InlineData(2, 2)]
[InlineData(1, 1)]
[InlineData(0, 0)]
public async Task EvaluateAsync_ZeroOne_AwardsProportionalCredit(
    int correctSelections,
    decimal expectedPoints)
{
    await using var db = CreateDb();

    var seed = await SeedQuestionAsync(db);

    var selectedIds = GetSelectionsWithCorrectCount(
        seed,
        correctSelections);

    var evaluator = new BowTieEvaluator(db);

    var result = await evaluator.EvaluateAsync(
        new StudentAnswerRequest
        {
            QuestionId = seed.Question.Id,
            BowTieOptionIds = selectedIds
        });

    Assert.NotNull(result);

    Assert.Equal(5, selectedIds.Count);
    Assert.Equal(5, selectedIds.Distinct().Count());

    Assert.Equal(correctSelections == 5, result.IsCorrect);
    Assert.Equal(expectedPoints, result.PointsEarned);
    Assert.Equal(5m, result.MaximumPoints);
}

    // ==========================================
    // ALL-OR-NOTHING SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, 0)]
    public async Task EvaluateAsync_AllOrNothing_ReturnsExpectedScore(
        bool allCorrect,
        decimal expectedPoints)
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(
            db,
            QuestionScoringMethod.AllOrNothing);

        var selectedIds = GetCorrectSelections(seed);

        if (!allCorrect)
        {
            selectedIds[4] = seed.DistractorParameter1.Id;
        }

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = selectedIds
            });

        Assert.NotNull(result);
        Assert.Equal(allCorrect, result.IsCorrect);
        Assert.Equal(expectedPoints, result.PointsEarned);
    }

    // ==========================================
    // STRUCTURAL VALIDATION
    // ==========================================

    [Fact]
    public async Task EvaluateAsync_MissingSelection_ReturnsNull()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        var selectedIds = GetCorrectSelections(seed);
        selectedIds.RemoveAt(4);

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = selectedIds
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateAsync_DuplicateSelection_ReturnsNull()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        var selectedIds = GetCorrectSelections(seed);
        selectedIds[4] = selectedIds[3];

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = selectedIds
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateAsync_InvalidCategoryDistribution_ReturnsNull()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        var selectedIds = new List<Guid>
        {
            seed.Condition.Id,
            seed.Action1.Id,
            seed.Action2.Id,
            seed.DistractorAction1.Id,
            seed.Parameter1.Id
        };

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = selectedIds
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateAsync_ForeignQuestionOption_ReturnsNull()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        var foreignOption = CreateOption(
            Guid.NewGuid(),
            BowTieOptionCategory.Parameter,
            false,
            1);

        db.BowTieOptions.Add(foreignOption);
        await db.SaveChangesAsync();

        var selectedIds = GetCorrectSelections(seed);
        selectedIds[4] = foreignOption.Id;

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = selectedIds
            });

        Assert.Null(result);
    }

    // ==========================================
    // ANSWER KEY VALIDATION
    // ==========================================

    [Fact]
    public async Task EvaluateAsync_InvalidAnswerKeyDistribution_ReturnsNull()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(db);

        // The question now has only one correct Action.
        seed.Action2.IsCorrect = false;

        await db.SaveChangesAsync();

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = seed.Question.Id,
                BowTieOptionIds = GetCorrectSelections(seed)
            });

        Assert.Null(result);
    }

    // ==========================================
    // QUESTION VALIDATION
    // ==========================================

    [Fact]
    public async Task EvaluateAsync_NonexistentQuestion_ReturnsNull()
    {
        await using var db = CreateDb();

        var evaluator = new BowTieEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = Guid.NewGuid(),
                BowTieOptionIds =
                [
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid()
                ]
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateAsync_UnsupportedScoringMethod_Throws()
    {
        await using var db = CreateDb();

        var seed = await SeedQuestionAsync(
            db,
            QuestionScoringMethod.PlusMinus);

        var evaluator = new BowTieEvaluator(db);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = seed.Question.Id,
                    BowTieOptionIds = GetCorrectSelections(seed)
                }));
    }
}