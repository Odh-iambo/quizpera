using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionNavigationService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionNavigationService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionNavigationResponse?> GetQuestionAsync(
        Guid sessionId,
        int position)
    {
        var session = await _db.ExamSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            return null;
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Questions can only be retrieved from an exam session that is in progress.");
        }

        if (position < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Question position must be at least 1.");
        }

        var totalQuestions = await _db.ExamQuestions
            .CountAsync(eq => eq.ExamId == session.ExamId);

        if (position > totalQuestions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Question position must be between 1 and {totalQuestions}.");
        }

        var examQuestion = await _db.ExamQuestions
            .Where(eq => eq.ExamId == session.ExamId)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .Skip(position - 1)
            .Select(eq => new
            {
                eq.QuestionId,
                eq.DisplayOrder,
                eq.Question.Stem,
                eq.Question.Difficulty,

        IsAnswered = _db.ExamSessionResponses.Any(r =>
    r.ExamSessionId == sessionId &&
    r.QuestionId == eq.QuestionId),

                Options = eq.Question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new ExamSessionQuestionOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (examQuestion is null)
        {
            return null;
        }

        return new ExamSessionNavigationResponse
        {
            SessionId = session.Id,
            CurrentPosition = position,
            TotalQuestions = totalQuestions,
            IsAnswered = examQuestion.IsAnswered,
            Question = new ExamSessionQuestionResponse
            {
                QuestionId = examQuestion.QuestionId,
                DisplayOrder = examQuestion.DisplayOrder,
                Stem = examQuestion.Stem,
                Difficulty = examQuestion.Difficulty.ToString(),
                Options = examQuestion.Options
            }
        };
    }
}