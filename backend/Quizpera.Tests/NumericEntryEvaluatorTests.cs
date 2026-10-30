using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services.Evaluation;
using Xunit;

namespace Quizpera.Tests;

public class NumericEntryEvaluatorTests
{
    private static async Task<(QuizperaDbContext Db, NumericEntryEvaluator Evaluator)>
        CreateEvaluatorAsync(decimal maximumPoints = 1m)
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"numeric-entry-tests-{Guid.NewGuid()}")
            .Options;

        var db = new QuizperaDbContext(options);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.NumericEntry,
            Stem = "Calculate the result.",
            ExamProgramId = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            Difficulty = Difficulty.Medium,
            NumericCorrectAnswer = 12.5m,
            NumericTolerance = 0.5m,
            MaximumPoints = maximumPoints
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync();

        return (db, new NumericEntryEvaluator(db));
    }

    [Fact]
    public async Task EvaluateAsync_ExactAnswer_ReturnsCorrect()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = 12.5m
                });

            Assert.NotNull(result);
            Assert.True(result.IsCorrect);
            Assert.Equal(12.5m, result.NumericAnswer);
            Assert.Equal(1m, result.PointsEarned);
            Assert.Equal(1m, result.MaximumPoints);
        }
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task EvaluateAsync_UsesConfiguredMaximumPoints(
        bool shouldBeCorrect,
        int expectedPoints)
    {
        var (db, evaluator) = await CreateEvaluatorAsync(maximumPoints: 3m);

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = shouldBeCorrect ? 12.5m : 20m
                });

            Assert.NotNull(result);
            Assert.Equal(shouldBeCorrect, result.IsCorrect);
            Assert.Equal((decimal)expectedPoints, result.PointsEarned);
            Assert.Equal(3m, result.MaximumPoints);
        }
    }

    [Fact]
    public async Task EvaluateAsync_AnswerWithinTolerance_ReturnsCorrect()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = 12.8m
                });

            Assert.NotNull(result);
            Assert.True(result.IsCorrect);
        }
    }

    [Fact]
    public async Task EvaluateAsync_AnswerAtToleranceBoundary_ReturnsCorrect()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = 13.0m
                });

            Assert.NotNull(result);
            Assert.True(result.IsCorrect);
        }
    }

    [Fact]
    public async Task EvaluateAsync_AnswerOutsideTolerance_ReturnsIncorrect()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = 13.1m
                });

            Assert.NotNull(result);
            Assert.False(result.IsCorrect);
            Assert.Equal(0m, result.PointsEarned);
            Assert.Equal(1m, result.MaximumPoints);
        }
    }

    [Fact]
    public async Task EvaluateAsync_MissingAnswer_ReturnsNull()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = null
                });

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task EvaluateAsync_NonexistentQuestion_ReturnsNull()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = Guid.NewGuid(),
                    NumericAnswer = 12.5m
                });

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task EvaluateAsync_MissingCorrectAnswer_ReturnsNull()
    {
        var (db, evaluator) = await CreateEvaluatorAsync();

        using (db)
        {
            var question = db.Questions.Local.Single();
            question.NumericCorrectAnswer = null;

            await db.SaveChangesAsync();

            var result = await evaluator.EvaluateAsync(
                new StudentAnswerRequest
                {
                    QuestionId = question.Id,
                    NumericAnswer = 12.5m
                });

            Assert.Null(result);
        }
    }
}
