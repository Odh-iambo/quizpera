using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using System.Globalization;

namespace Quizpera.Tests;

public class ExamSessionResultServiceTests
{
    [Theory]
    [InlineData("TTT", 3, 3, 0, 0, "100.00")]
    [InlineData("TT", 2, 2, 0, 1, "66.67")]
    [InlineData("TF", 2, 1, 1, 1, "33.33")]
    [InlineData("FFF", 3, 0, 3, 0, "0.00")]
    [InlineData("", 0, 0, 0, 3, "")]
    public async Task GetResultAsync_ReturnsCorrectScoreAndCounts(
        string answers,
        int expectedAnswered,
        int expectedCorrect,
        int expectedIncorrect,
        int expectedUnanswered,
        string expectedScore)
    {
        // Arrange: create an isolated SQLite in-memory database.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new QuizperaDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var sessionId = await SeedExamAsync(db, answers);

        var service = new ExamSessionResultService(db);

        // Act: call the real application service.
        var result = await service.GetResultAsync(sessionId);

        // Assert: verify the complete scoring result.
        Assert.NotNull(result);

        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("Completed", result.Status);

        Assert.Equal(3, result.TotalQuestions);
        Assert.Equal(expectedAnswered, result.AnsweredQuestions);
        Assert.Equal(expectedCorrect, result.CorrectAnswers);
        Assert.Equal(expectedIncorrect, result.IncorrectAnswers);
        Assert.Equal(expectedUnanswered, result.UnansweredQuestions);

        if (string.IsNullOrEmpty(expectedScore))
        {
            Assert.Null(result.ScorePercentage);
        }
        else
        {
            var score = decimal.Parse(
                expectedScore,
                CultureInfo.InvariantCulture);

            Assert.Equal(score, result.ScorePercentage);
        }
    }



        [Fact]
    public async Task GetResultAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new QuizperaDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var service = new ExamSessionResultService(db);
        var nonexistentSessionId = Guid.NewGuid();

        // Act
        var result = await service.GetResultAsync(nonexistentSessionId);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task GetResultAsync_WhenExamHasNoQuestions_ReturnsNullScore()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new QuizperaDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Empty Exam Test Program"
        };

        db.ExamPrograms.Add(program);
        await db.SaveChangesAsync();

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Empty Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id
        };

        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Status = ExamSessionStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();

        var service = new ExamSessionResultService(db);

        // Act
        var result = await service.GetResultAsync(session.Id);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal("Completed", result.Status);

        Assert.Equal(0, result.TotalQuestions);
        Assert.Equal(0, result.AnsweredQuestions);
        Assert.Equal(0, result.CorrectAnswers);
        Assert.Equal(0, result.IncorrectAnswers);
        Assert.Equal(0, result.UnansweredQuestions);

        Assert.Null(result.ScorePercentage);
    }
    private static async Task<Guid> SeedExamAsync(
        QuizperaDbContext db,
        string answers)
    {
        // Create the required program, domain, and subject.
        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Test Nursing Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Test Domain",
            ExamProgramId = program.Id
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Test Subject",
            DomainId = domain.Id
        };

        db.AddRange(program, domain, subject);

        await db.SaveChangesAsync();

        // Create an exam.
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Scoring Test Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id
        };

        db.Exams.Add(exam);

        // Create three questions, each with one correct
        // and one incorrect option.
        var questions = new List<Question>();
        var correctOptionIds = new List<Guid>();
        var incorrectOptionIds = new List<Guid>();

        for (var i = 0; i < 3; i++)
        {
            var question = new Question
            {
                Id = Guid.NewGuid(),
                Type = QuestionType.SingleChoice,
                Stem = $"Test question {i + 1}",
                ExamProgramId = program.Id,
                DomainId = domain.Id,
                SubjectId = subject.Id,
                Difficulty = Difficulty.Easy
            };

            var correctOption = new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = "Correct answer",
                DisplayOrder = 1,
                IsCorrect = true
            };

            var incorrectOption = new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = "Incorrect answer",
                DisplayOrder = 2,
                IsCorrect = false
            };

            questions.Add(question);
            correctOptionIds.Add(correctOption.Id);
            incorrectOptionIds.Add(incorrectOption.Id);

            db.Questions.Add(question);

            db.QuestionOptions.AddRange(
                correctOption,
                incorrectOption);

            db.ExamQuestions.Add(new ExamQuestion
            {
                ExamId = exam.Id,
                QuestionId = question.Id,
                DisplayOrder = i + 1
            });
        }

        await db.SaveChangesAsync();

        // Create a completed session.
        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Status = ExamSessionStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        db.ExamSessions.Add(session);

        await db.SaveChangesAsync();

        // T = correct answer, F = incorrect answer.
        // Missing characters represent unanswered questions.
        for (var i = 0; i < answers.Length; i++)
        {
            var isCorrect = answers[i] == 'T';

            db.ExamSessionResponses.Add(new ExamSessionResponse
            {
                Id = Guid.NewGuid(),
                ExamSessionId = session.Id,
                QuestionId = questions[i].Id,
                SelectedOptionId = isCorrect
                    ? correctOptionIds[i]
                    : incorrectOptionIds[i],
                IsCorrect = isCorrect,
                AnsweredAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        return session.Id;
    }
}
