using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamSessionReviewServiceTests
{
    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"review-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsSelectedAndCorrectRationalesAndScore()
    {
        await using var db = CreateDbContext();

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
            ExamProgram = program
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
            Name = "Rationale Review Exam",
            Type = ExamType.Custom,
            ExamProgramId = program.Id,
            ExamProgram = program
        };

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            Exam = exam,
            Status = ExamSessionStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow
        };

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.SingleChoice,
            ScoringMethod = QuestionScoringMethod.RationaleScoring,
            Stem = "Which intervention is appropriate?",
            ExamProgramId = program.Id,
            ExamProgram = program,
            DomainId = domain.Id,
            Domain = domain,
            SubjectId = subject.Id,
            Subject = subject,
            Difficulty = Difficulty.Medium,
            MaximumPoints = 2m,
            Rationale = "Review the clinical reasoning."
        };

        var correctAnswer = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            Text = "Correct intervention",
            IsCorrect = true
        };

        var incorrectAnswer = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            Text = "Incorrect intervention",
            IsCorrect = false
        };

        var correctRationale = new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            Text = "Correct clinical reasoning",
            DisplayOrder = 1,
            IsCorrect = true
        };

        var incorrectRationale = new QuestionRationaleOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            Text = "Incorrect clinical reasoning",
            DisplayOrder = 2,
            IsCorrect = false
        };

        question.Options.Add(correctAnswer);
        question.Options.Add(incorrectAnswer);
        question.RationaleOptions.Add(correctRationale);
        question.RationaleOptions.Add(incorrectRationale);

        var response = new ExamSessionResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = session.Id,
            ExamSession = session,
            QuestionId = question.Id,
            Question = question,
            SelectedOptionId = correctAnswer.Id,
            IsCorrect = true,
            PointsEarned = 1m,
            MaximumPoints = 2m,
            AnsweredAt = DateTime.UtcNow
        };

        response.SelectedRationaleOptions.Add(
            new ExamSessionResponseRationaleOption
            {
                ExamSessionResponseId = response.Id,
                ExamSessionResponse = response,
                QuestionRationaleOptionId = incorrectRationale.Id,
                QuestionRationaleOption = incorrectRationale
            });

        db.AddRange(
            program,
            domain,
            subject,
            exam,
            session,
            question,
            correctAnswer,
            incorrectAnswer,
            correctRationale,
            incorrectRationale,
            response);

        db.ExamQuestions.Add(new ExamQuestion
        {
            ExamId = exam.Id,
            Exam = exam,
            QuestionId = question.Id,
            Question = question,
            DisplayOrder = 1
        });

        await db.SaveChangesAsync();

        var service = new ExamSessionReviewService(db);

        var result = await service.GetReviewAsync(session.Id);

        Assert.NotNull(result);
        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal(nameof(ExamSessionStatus.Completed), result.Status);

        var reviewedQuestion = Assert.Single(result.Questions);

        Assert.Equal(question.Id, reviewedQuestion.QuestionId);
        Assert.Equal(correctAnswer.Id, reviewedQuestion.SelectedOption!.Id);
        Assert.Equal(correctAnswer.Id, reviewedQuestion.CorrectOption!.Id);

        var selectedRationale = Assert.Single(
            reviewedQuestion.SelectedRationaleOptions);

        Assert.Equal(incorrectRationale.Id, selectedRationale.Id);
        Assert.Equal("Incorrect clinical reasoning", selectedRationale.Text);
        Assert.False(selectedRationale.IsCorrect);

        var expectedCorrectRationale = Assert.Single(
            reviewedQuestion.CorrectRationaleOptions);

        Assert.Equal(correctRationale.Id, expectedCorrectRationale.Id);
        Assert.True(expectedCorrectRationale.IsCorrect);

        Assert.Equal(1m, reviewedQuestion.PointsEarned);
        Assert.Equal(2m, reviewedQuestion.MaximumPoints);
        Assert.True(reviewedQuestion.IsCorrect);
        Assert.Equal("Review the clinical reasoning.", reviewedQuestion.Rationale);
    }

    [Fact]
    public async Task GetReviewAsync_ThrowsWhenSessionIsNotCompleted()
    {
        await using var db = CreateDbContext();

        var program = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "Incomplete Review Program"
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Name = "Incomplete Review Exam",
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

        db.AddRange(program, exam, session);
        await db.SaveChangesAsync();

        var service = new ExamSessionReviewService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetReviewAsync(session.Id));

        Assert.Contains("only available after", exception.Message);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNullWhenSessionDoesNotExist()
    {
        await using var db = CreateDbContext();

        var service = new ExamSessionReviewService(db);

        var result = await service.GetReviewAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}

