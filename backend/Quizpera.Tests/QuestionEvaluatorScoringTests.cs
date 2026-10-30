
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services.Evaluation;
using Xunit;

namespace Quizpera.Tests;

public class QuestionEvaluatorScoringTests
{


        [Fact]
    public void Question_DefaultsToAllOrNothingScoring()
    {
        var question = new Question();

        Assert.Equal(
            QuestionScoringMethod.AllOrNothing,
            question.ScoringMethod);
    }
    
    // ==========================================
    // SINGLE CHOICE: DEFAULT SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task SingleChoice_ReturnsExpectedScore(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.SingleChoice);
        var option = CreateOption(question.Id, isCorrect);

        db.Questions.Add(question);
        db.QuestionOptions.Add(option);
        await db.SaveChangesAsync();

        var evaluator = new SingleChoiceEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(1m, result.MaximumPoints);
    }

    // ==========================================
    // SINGLE CHOICE: WEIGHTED SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task SingleChoice_UsesConfiguredMaximumPoints(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.SingleChoice,
            maximumPoints: 3m);

        var option = CreateOption(question.Id, isCorrect);

        db.Questions.Add(question);
        db.QuestionOptions.Add(option);
        await db.SaveChangesAsync();

        var evaluator = new SingleChoiceEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(3m, result.MaximumPoints);
    }
    

    // ==========================================
    // SINGLE CHOICE: RATIONALE SCORING
    // ==========================================

    [Theory]
    [InlineData(true, true, 2)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, 0)]
    public async Task SingleChoice_RationaleScoring_ReturnsExpectedScore(
        bool answerIsCorrect,
        bool rationaleIsCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.SingleChoice,
            maximumPoints: 2m);

        question.ScoringMethod = QuestionScoringMethod.RationaleScoring;

        var answerOption = CreateOption(
            question.Id,
            isCorrect: answerIsCorrect);

        var rationaleOption = new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Text = "Test rationale",
            DisplayOrder = 1,
            IsCorrect = rationaleIsCorrect
        };

        question.RationaleOptions.Add(rationaleOption);

        db.Questions.Add(question);
        db.QuestionOptions.Add(answerOption);

        await db.SaveChangesAsync();

        var evaluator = new SingleChoiceEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = answerOption.Id,
                SelectedRationaleOptionIds = new List<Guid>
                {
                    rationaleOption.Id
                }
            });

        Assert.NotNull(result);

        // IsCorrect reflects the primary answer's correctness.
        Assert.Equal(answerIsCorrect, result.IsCorrect);

        // Two points maximum: one for the answer and one for the rationale.
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(2m, result.MaximumPoints);

        Assert.Single(result.SelectedRationaleOptionIds);
        Assert.Equal(
            rationaleOption.Id,
            result.SelectedRationaleOptionIds[0]);
    }

    [Fact]
    public async Task SingleChoice_RationaleScoring_RejectsMissingRationale()
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.SingleChoice);
        question.ScoringMethod = QuestionScoringMethod.RationaleScoring;

        var answerOption = CreateOption(
            question.Id,
            isCorrect: true);

        question.RationaleOptions.Add(new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Text = "Correct rationale",
            DisplayOrder = 1,
            IsCorrect = true
        });

        db.Questions.Add(question);
        db.QuestionOptions.Add(answerOption);

        await db.SaveChangesAsync();

        var evaluator = new SingleChoiceEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = answerOption.Id,
                SelectedRationaleOptionIds = new List<Guid>()
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task SingleChoice_RationaleScoring_RejectsRationaleFromAnotherQuestion()
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.SingleChoice);
        question.ScoringMethod = QuestionScoringMethod.RationaleScoring;

        var answerOption = CreateOption(
            question.Id,
            isCorrect: true);

        var foreignRationaleId = Guid.NewGuid();

        question.RationaleOptions.Add(new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Text = "Available rationale",
            DisplayOrder = 1,
            IsCorrect = true
        });

        db.Questions.Add(question);
        db.QuestionOptions.Add(answerOption);

        await db.SaveChangesAsync();

        var evaluator = new SingleChoiceEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = answerOption.Id,
                SelectedRationaleOptionIds = new List<Guid>
                {
                    foreignRationaleId
                }
            });

        Assert.Null(result);
    }

    
[Fact]
public async Task SingleChoice_UnsupportedScoringMethod_Throws()
{
    await using var db = CreateDb();

    var question = CreateQuestion(QuestionType.SingleChoice);
    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var option = CreateOption(question.Id, isCorrect: true);

    db.Questions.Add(question);
    db.QuestionOptions.Add(option);
    await db.SaveChangesAsync();

    var evaluator = new SingleChoiceEvaluator(db);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            }));
}

    // TRUE/FALSE: DEFAULT SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task TrueFalse_ReturnsExpectedScore(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.TrueFalse);
        var option = CreateOption(question.Id, isCorrect);

        db.Questions.Add(question);
        db.QuestionOptions.Add(option);
        await db.SaveChangesAsync();

        var evaluator = new TrueFalseEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(1m, result.MaximumPoints);
    }

    // ==========================================
    // TRUE/FALSE: WEIGHTED SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task TrueFalse_UsesConfiguredMaximumPoints(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.TrueFalse,
            maximumPoints: 3m);

        var option = CreateOption(question.Id, isCorrect);

        db.Questions.Add(question);
        db.QuestionOptions.Add(option);
        await db.SaveChangesAsync();

        var evaluator = new TrueFalseEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(3m, result.MaximumPoints);
    }

    // ==========================================
    // MULTIPLE RESPONSE: DEFAULT SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task MultipleResponse_ReturnsExpectedScore(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.MultipleResponse);

        var correctOption1 = CreateOption(question.Id, true);
        var correctOption2 = CreateOption(question.Id, true);
        var incorrectOption = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption1,
            correctOption2,
            incorrectOption);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var selectedOptionIds = isCorrect
            ? new List<Guid>
            {
                correctOption1.Id,
                correctOption2.Id
            }
            : new List<Guid>
            {
                correctOption1.Id,
                incorrectOption.Id
            };

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionIds = selectedOptionIds
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(1m, result.MaximumPoints);
    }

    // ==========================================
    // MULTIPLE RESPONSE: WEIGHTED SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task MultipleResponse_UsesConfiguredMaximumPoints(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.MultipleResponse,
            maximumPoints: 3m);

        var correctOption1 = CreateOption(question.Id, true);
        var correctOption2 = CreateOption(question.Id, true);
        var incorrectOption = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption1,
            correctOption2,
            incorrectOption);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var selectedOptionIds = isCorrect
            ? new List<Guid>
            {
                correctOption1.Id,
                correctOption2.Id
            }
            : new List<Guid>
            {
                correctOption1.Id,
                incorrectOption.Id
            };

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionIds = selectedOptionIds
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(3m, result.MaximumPoints);
    }
    


        // ==========================================
    // MULTIPLE RESPONSE: PLUS/MINUS SCORING
    // ==========================================

    [Fact]
    public async Task MultipleResponse_PlusMinus_AllCorrectOptionsEarnMaximumPoints()
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.MultipleResponse,
            maximumPoints: 2m);

        question.ScoringMethod = QuestionScoringMethod.PlusMinus;

        var correctOption1 = CreateOption(question.Id, true);
        var correctOption2 = CreateOption(question.Id, true);
        var incorrectOption = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption1,
            correctOption2,
            incorrectOption);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionIds = new List<Guid>
                {
                    correctOption1.Id,
                    correctOption2.Id
                }
            });

        Assert.NotNull(result);
        Assert.True(result.IsCorrect);
        Assert.Equal(2m, result.PointsEarned);
        Assert.Equal(2m, result.MaximumPoints);
    }

    [Fact]
    public async Task MultipleResponse_PlusMinus_PartiallyCorrectAnswerEarnsPartialCredit()
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.MultipleResponse,
            maximumPoints: 2m);

        question.ScoringMethod = QuestionScoringMethod.PlusMinus;

        var correctOption1 = CreateOption(question.Id, true);
        var correctOption2 = CreateOption(question.Id, true);
        var incorrectOption = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption1,
            correctOption2,
            incorrectOption);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionIds = new List<Guid>
                {
                    correctOption1.Id
                }
            });

        Assert.NotNull(result);
        Assert.False(result.IsCorrect);
        Assert.Equal(1m, result.PointsEarned);
        Assert.Equal(2m, result.MaximumPoints);
    }


    [Fact]
public async Task MultipleResponse_PlusMinus_ThreeCorrectOptionsPreservesFractionalScore()
{
    await using var db = CreateDb();

    var question = CreateQuestion(
        QuestionType.MultipleResponse,
        maximumPoints: 1m);

    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var correctOption1 = CreateOption(question.Id, true);
    var correctOption2 = CreateOption(question.Id, true);
    var correctOption3 = CreateOption(question.Id, true);
    var incorrectOption = CreateOption(question.Id, false);

    db.Questions.Add(question);

    db.QuestionOptions.AddRange(
        correctOption1,
        correctOption2,
        correctOption3,
        incorrectOption);

    await db.SaveChangesAsync();

    var evaluator = new MultipleResponseEvaluator(db);

    var result = await evaluator.EvaluateAsync(
        new StudentAnswerRequest
        {
            QuestionId = question.Id,
            SelectedOptionIds = new List<Guid>
            {
                correctOption1.Id,
                correctOption2.Id
            }
        });

    Assert.NotNull(result);
    Assert.False(result.IsCorrect);
    Assert.Equal(1m, result.MaximumPoints);

    Assert.InRange(
        result.PointsEarned,
        0m,
        result.MaximumPoints);

    Assert.InRange(
    result.PointsEarned,
    0.6666666666666666666666666665m,
    0.6666666666666666666666666667m);
}




    [Fact]
    public async Task MultipleResponse_PlusMinus_IncorrectSelectionsCannotProduceNegativeScore()
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.MultipleResponse,
            maximumPoints: 2m);

        question.ScoringMethod = QuestionScoringMethod.PlusMinus;

        var correctOption = CreateOption(question.Id, true);
        var incorrectOption1 = CreateOption(question.Id, false);
        var incorrectOption2 = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption,
            incorrectOption1,
            incorrectOption2);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionIds = new List<Guid>
                {
                    incorrectOption1.Id,
                    incorrectOption2.Id
                }
            });

        Assert.NotNull(result);
        Assert.False(result.IsCorrect);
        Assert.Equal(0m, result.PointsEarned);
        Assert.Equal(2m, result.MaximumPoints);
    }

[Fact]
public async Task MultipleResponse_PlusMinus_MixedFractionalSelectionsApplyPenalty()
{
    await using var db = CreateDb();

    var question = CreateQuestion(
        QuestionType.MultipleResponse,
        maximumPoints: 1m);

    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var correctOption1 = CreateOption(question.Id, true);
    var correctOption2 = CreateOption(question.Id, true);
    var correctOption3 = CreateOption(question.Id, true);
    var incorrectOption = CreateOption(question.Id, false);

    db.Questions.Add(question);

    db.QuestionOptions.AddRange(
        correctOption1,
        correctOption2,
        correctOption3,
        incorrectOption);

    await db.SaveChangesAsync();

    var evaluator = new MultipleResponseEvaluator(db);

    var result = await evaluator.EvaluateAsync(
        new StudentAnswerRequest
        {
            QuestionId = question.Id,
            SelectedOptionIds = new List<Guid>
            {
                correctOption1.Id,
                correctOption2.Id,
                incorrectOption.Id
            }
        });

    Assert.NotNull(result);
    Assert.False(result.IsCorrect);
    Assert.Equal(1m, result.MaximumPoints);

    Assert.InRange(
        result.PointsEarned,
        0.3333333333333333333333333332m,
        0.3333333333333333333333333334m);
}

    [Fact]
public async Task MultipleResponse_PlusMinus_PenalizesIncorrectSelection()
{
    await using var db = CreateDb();

    var question = CreateQuestion(
        QuestionType.MultipleResponse,
        maximumPoints: 2m);

    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var correctOption1 = CreateOption(question.Id, true);
    var correctOption2 = CreateOption(question.Id, true);
    var incorrectOption = CreateOption(question.Id, false);

    db.Questions.Add(question);

    db.QuestionOptions.AddRange(
        correctOption1,
        correctOption2,
        incorrectOption);

    await db.SaveChangesAsync();

    var evaluator = new MultipleResponseEvaluator(db);

    var result = await evaluator.EvaluateAsync(
        new StudentAnswerRequest
        {
            QuestionId = question.Id,
            SelectedOptionIds = new List<Guid>
            {
                correctOption1.Id,
                correctOption2.Id,
                incorrectOption.Id
            }
        });

    Assert.NotNull(result);
    Assert.False(result.IsCorrect);
    Assert.Equal(1m, result.PointsEarned);
    Assert.Equal(2m, result.MaximumPoints);
}


[Theory]
[InlineData(1, false)]
[InlineData(2, false)]
[InlineData(3, false)]
[InlineData(1, true)]
public async Task MultipleResponse_ZeroOne_AwardsProportionalCredit(
    int correctSelections,
    bool includeIncorrectSelection)
{
    await using var db = CreateDb();

    var question = CreateQuestion(
        QuestionType.MultipleResponse,
        maximumPoints: 1m);

    question.ScoringMethod = QuestionScoringMethod.ZeroOne;

    var correctOption1 = CreateOption(question.Id, true);
    var correctOption2 = CreateOption(question.Id, true);
    var correctOption3 = CreateOption(question.Id, true);
    var incorrectOption = CreateOption(question.Id, false);

    db.Questions.Add(question);

    db.QuestionOptions.AddRange(
        correctOption1,
        correctOption2,
        correctOption3,
        incorrectOption);

    await db.SaveChangesAsync();

    var correctOptions = new[]
    {
        correctOption1,
        correctOption2,
        correctOption3
    };

    var selectedOptionIds = correctOptions
        .Take(correctSelections)
        .Select(option => option.Id)
        .ToList();

    if (includeIncorrectSelection)
    {
        selectedOptionIds.Add(incorrectOption.Id);
    }

    var evaluator = new MultipleResponseEvaluator(db);

    var result = await evaluator.EvaluateAsync(
        new StudentAnswerRequest
        {
            QuestionId = question.Id,
            SelectedOptionIds = selectedOptionIds
        });

    Assert.NotNull(result);

    Assert.Equal(
        correctSelections == 3 && !includeIncorrectSelection,
        result.IsCorrect);

    Assert.InRange(
    result.PointsEarned,
    (correctSelections / 3m) - 0.0000000000000000000000000001m,
    (correctSelections / 3m) + 0.0000000000000000000000000001m);

    Assert.Equal(1m, result.MaximumPoints);
}

        [Theory]
    [InlineData(QuestionScoringMethod.RationaleScoring)]
    public async Task MultipleResponse_UnsupportedScoringMethod_Throws(
        QuestionScoringMethod scoringMethod)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.MultipleResponse);

        question.ScoringMethod = scoringMethod;

        var correctOption = CreateOption(question.Id, true);
        var incorrectOption = CreateOption(question.Id, false);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(
            correctOption,
            incorrectOption);

        await db.SaveChangesAsync();

        var evaluator = new MultipleResponseEvaluator(db);

        var request = new StudentAnswerRequest
        {
            QuestionId = question.Id,
            SelectedOptionIds = new List<Guid>
            {
                correctOption.Id
            }
        };

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await evaluator.EvaluateAsync(request));
    }

    // ==========================================
    // ORDERED RESPONSE: DEFAULT SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task OrderedResponse_ReturnsExpectedScore(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(QuestionType.OrderedResponse);

        var first = CreateOption(
            question.Id,
            false,
            correctOrder: 1);

        var second = CreateOption(
            question.Id,
            false,
            correctOrder: 2);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(first, second);
        await db.SaveChangesAsync();

        var evaluator = new OrderedResponseEvaluator(db);

        var submittedOrder = isCorrect
            ? new List<Guid> { first.Id, second.Id }
            : new List<Guid> { second.Id, first.Id };

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                OrderedOptionIds = submittedOrder
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(1m, result.MaximumPoints);
    }

    // ==========================================
    // ORDERED RESPONSE: WEIGHTED SCORING
    // ==========================================

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task OrderedResponse_UsesConfiguredMaximumPoints(
        bool isCorrect,
        int expectedPoints)
    {
        await using var db = CreateDb();

        var question = CreateQuestion(
            QuestionType.OrderedResponse,
            maximumPoints: 3m);

        var first = CreateOption(
            question.Id,
            false,
            correctOrder: 1);

        var second = CreateOption(
            question.Id,
            false,
            correctOrder: 2);

        db.Questions.Add(question);
        db.QuestionOptions.AddRange(first, second);
        await db.SaveChangesAsync();

        var evaluator = new OrderedResponseEvaluator(db);

        var submittedOrder = isCorrect
            ? new List<Guid> { first.Id, second.Id }
            : new List<Guid> { second.Id, first.Id };

        var result = await evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                OrderedOptionIds = submittedOrder
            });

        Assert.NotNull(result);
        Assert.Equal(isCorrect, result.IsCorrect);
        Assert.Equal((decimal)expectedPoints, result.PointsEarned);
        Assert.Equal(3m, result.MaximumPoints);
    }



[Fact]
public async Task TrueFalse_UnsupportedScoringMethod_Throws()
{
    await using var db = CreateDb();

    var question = CreateQuestion(QuestionType.TrueFalse);
    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var option = CreateOption(question.Id, isCorrect: true);

    db.Questions.Add(question);
    db.QuestionOptions.Add(option);
    await db.SaveChangesAsync();

    var evaluator = new TrueFalseEvaluator(db);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                SelectedOptionId = option.Id
            }));
}

[Fact]
public async Task NumericEntry_UnsupportedScoringMethod_Throws()
{
    await using var db = CreateDb();

    var question = CreateQuestion(QuestionType.NumericEntry);
    question.ScoringMethod = QuestionScoringMethod.PlusMinus;
    question.NumericCorrectAnswer = 12.5m;

    db.Questions.Add(question);
    await db.SaveChangesAsync();

    var evaluator = new NumericEntryEvaluator(db);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                NumericAnswer = 12.5m
            }));
}

[Fact]
public async Task OrderedResponse_UnsupportedScoringMethod_Throws()
{
    await using var db = CreateDb();

    var question = CreateQuestion(QuestionType.OrderedResponse);
    question.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var first = CreateOption(question.Id, isCorrect: true, correctOrder: 1);
    var second = CreateOption(question.Id, isCorrect: false, correctOrder: 2);

    db.Questions.Add(question);
    db.QuestionOptions.AddRange(first, second);
    await db.SaveChangesAsync();

    var evaluator = new OrderedResponseEvaluator(db);

    await Assert.ThrowsAsync<NotSupportedException>(() =>
        evaluator.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = question.Id,
                OrderedOptionIds = [first.Id, second.Id]
            }));
}


    // ==========================================
    // TEST DATABASE
    // ==========================================

    private static QuizperaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"scoring-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    // ==========================================
    // TEST QUESTION FACTORY
    // ==========================================

    private static Question CreateQuestion(
        QuestionType type,
        decimal maximumPoints = 1m)
    {
        return new Question
        {
            Id = Guid.NewGuid(),
            Type = type,
            Stem = "Test question",
            ExamProgramId = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            Difficulty = Difficulty.Medium,
            MaximumPoints = maximumPoints
        };
    }

    // ==========================================
    // TEST OPTION FACTORY
    // ==========================================

    private static QuestionOption CreateOption(
        Guid questionId,
        bool isCorrect,
        int? correctOrder = null)
    {
        return new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Text = $"Option {Guid.NewGuid():N}",
            IsCorrect = isCorrect,
            CorrectOrder = correctOrder
        };
    }
}
