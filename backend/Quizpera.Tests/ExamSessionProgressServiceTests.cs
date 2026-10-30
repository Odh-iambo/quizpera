using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;

namespace Quizpera.Tests;

public class ExamSessionProgressServiceTests
{
    [Fact]
    public async Task GetProgressAsync_CountsExamQuestionsAndAnswersCorrectly()
    {
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
            Name = "Progress Test Program"
        };

        var domain = new Quizpera.Api.Domain.Questions.Domain
        {
            Id = Guid.NewGuid(),
            Name = "Progress Test Domain",
            ExamProgramId = program.Id
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Progress Test Subject",
            DomainId = domain.Id
        };

        db.AddRange(program, domain, subject);

        await db.SaveChangesAsync();

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Progress Test Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id
        };

        db.Exams.Add(exam);

        await db.SaveChangesAsync();

        var questions = Enumerable.Range(1, 4)
            .Select(i => new Question
            {
                Id = Guid.NewGuid(),
                Stem = $"Progress question {i}",
                Type = QuestionType.SingleChoice,
                ExamProgramId = program.Id,
                DomainId = domain.Id,
                SubjectId = subject.Id,
                Difficulty = Difficulty.Medium
            })
            .ToList();

        db.Questions.AddRange(questions);

        await db.SaveChangesAsync();

        // Every question needs valid options because responses reference
        // QuestionOption through SelectedOptionId.
        var optionsByQuestion = questions
            .Select(question => new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = "Test option",
                DisplayOrder = 1,
                IsCorrect = true
            })
            .ToList();

        db.QuestionOptions.AddRange(optionsByQuestion);

        await db.SaveChangesAsync();

        db.ExamQuestions.AddRange(
            questions.Select((question, index) => new ExamQuestion
            {
                ExamId = exam.Id,
                QuestionId = question.Id,
                DisplayOrder = index + 1
            }));

        await db.SaveChangesAsync();

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Status = ExamSessionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.ExamSessions.Add(session);

        await db.SaveChangesAsync();

        // Two distinct questions have responses in this session.
        db.ExamSessionResponses.AddRange(
            new ExamSessionResponse
            {
                Id = Guid.NewGuid(),
                ExamSessionId = session.Id,
                QuestionId = questions[0].Id,
                SelectedOptionId = optionsByQuestion[0].Id,
                IsCorrect = true,
                AnsweredAt = DateTime.UtcNow
            },
            new ExamSessionResponse
            {
                Id = Guid.NewGuid(),
                ExamSessionId = session.Id,
                QuestionId = questions[1].Id,
                SelectedOptionId = optionsByQuestion[1].Id,
                IsCorrect = true,
                AnsweredAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var service = new ExamSessionProgressService(db);

        var result = await service.GetProgressAsync(session.Id);

        Assert.NotNull(result);

        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal("InProgress", result.Status);

        Assert.Equal(4, result.TotalQuestions);
        Assert.Equal(2, result.AnsweredQuestions);
        Assert.Equal(2, result.UnansweredQuestions);
        Assert.Equal(50m, result.ProgressPercentage);
    }

    [Fact]
    public async Task GetProgressAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new QuizperaDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var service = new ExamSessionProgressService(db);

        var result = await service.GetProgressAsync(Guid.NewGuid());

        Assert.Null(result);
    }



    [Fact]
public async Task GetProgressAsync_IgnoresResponsesForQuestionsOutsideExam()
{
    await using var connection =
        new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");

    await connection.OpenAsync();

    var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<
        Quizpera.Api.Data.QuizperaDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new Quizpera.Api.Data.QuizperaDbContext(options);

    await db.Database.EnsureCreatedAsync();

    var program = new Quizpera.Api.Domain.Questions.ExamProgram
    {
        Id = Guid.NewGuid(),
        Name = "Progress Test Program"
    };

    var domain = new Quizpera.Api.Domain.Questions.Domain
    {
        Id = Guid.NewGuid(),
        Name = "Progress Test Domain",
        ExamProgramId = program.Id
    };

    var subject = new Quizpera.Api.Domain.Questions.Subject
    {
        Id = Guid.NewGuid(),
        Name = "Progress Test Subject",
        DomainId = domain.Id
    };

    db.AddRange(program, domain, subject);
    await db.SaveChangesAsync();

    var exam = new Quizpera.Api.Domain.Questions.Exam
    {
        Id = Guid.NewGuid(),
        Name = "Progress Test Exam",
        Type = Quizpera.Api.Domain.Questions.ExamType.Custom,
        ExamProgramId = program.Id
    };

    db.Exams.Add(exam);
    await db.SaveChangesAsync();

    var examQuestion = new Quizpera.Api.Domain.Questions.Question
    {
        Id = Guid.NewGuid(),
        Stem = "Question in exam",
        Type = Quizpera.Api.Domain.Questions.QuestionType.SingleChoice,
        ExamProgramId = program.Id,
        DomainId = domain.Id,
        SubjectId = subject.Id,
        Difficulty = Quizpera.Api.Domain.Questions.Difficulty.Medium
    };

    var outsideQuestion = new Quizpera.Api.Domain.Questions.Question
    {
        Id = Guid.NewGuid(),
        Stem = "Question outside exam",
        Type = Quizpera.Api.Domain.Questions.QuestionType.SingleChoice,
        ExamProgramId = program.Id,
        DomainId = domain.Id,
        SubjectId = subject.Id,
        Difficulty = Quizpera.Api.Domain.Questions.Difficulty.Medium
    };

    db.Questions.AddRange(examQuestion, outsideQuestion);
    await db.SaveChangesAsync();

    var examOption = new Quizpera.Api.Domain.Questions.QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = examQuestion.Id,
        Text = "Option A",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var outsideOption = new Quizpera.Api.Domain.Questions.QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = outsideQuestion.Id,
        Text = "Option A",
        DisplayOrder = 1,
        IsCorrect = true
    };

    db.QuestionOptions.AddRange(examOption, outsideOption);
    await db.SaveChangesAsync();

    db.ExamQuestions.Add(new Quizpera.Api.Domain.Questions.ExamQuestion
    {
        ExamId = exam.Id,
        QuestionId = examQuestion.Id,
        DisplayOrder = 1
    });

    await db.SaveChangesAsync();

    var session = new Quizpera.Api.Domain.Questions.ExamSession
    {
        Id = Guid.NewGuid(),
        ExamId = exam.Id,
        Status = Quizpera.Api.Domain.Questions.ExamSessionStatus.InProgress,
        StartedAt = DateTime.UtcNow
    };

    db.ExamSessions.Add(session);
    await db.SaveChangesAsync();

    db.ExamSessionResponses.AddRange(
        new Quizpera.Api.Domain.Questions.ExamSessionResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = session.Id,
            QuestionId = examQuestion.Id,
            SelectedOptionId = examOption.Id,
            IsCorrect = true,
            AnsweredAt = DateTime.UtcNow
        },
        new Quizpera.Api.Domain.Questions.ExamSessionResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = session.Id,
            QuestionId = outsideQuestion.Id,
            SelectedOptionId = outsideOption.Id,
            IsCorrect = true,
            AnsweredAt = DateTime.UtcNow
        });

    await db.SaveChangesAsync();

    var service = new Quizpera.Api.Services.ExamSessionProgressService(db);

    var result = await service.GetProgressAsync(session.Id);

    Assert.NotNull(result);
    Assert.Equal(1, result.TotalQuestions);
    Assert.Equal(1, result.AnsweredQuestions);
    Assert.Equal(0, result.UnansweredQuestions);
    Assert.Equal(100m, result.ProgressPercentage);
}
}