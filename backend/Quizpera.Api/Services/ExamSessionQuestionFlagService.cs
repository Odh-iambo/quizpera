using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionQuestionFlagService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionQuestionFlagService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<bool> FlagQuestionAsync(
        Guid sessionId,
        Guid questionId)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            throw new KeyNotFoundException(
                "The specified exam session was not found.");
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Questions can only be flagged while the exam session is in progress.");
        }

        var questionBelongsToExam = await _db.ExamQuestions
            .AnyAsync(eq =>
                eq.ExamId == session.ExamId &&
                eq.QuestionId == questionId);

        if (!questionBelongsToExam)
        {
            throw new KeyNotFoundException(
                "The specified question does not belong to this exam session.");
        }

        var alreadyFlagged = await _db.ExamSessionQuestionFlags
            .AnyAsync(f =>
                f.ExamSessionId == sessionId &&
                f.QuestionId == questionId);

        if (alreadyFlagged)
        {
            return false;
        }

        var flag = new ExamSessionQuestionFlag
        {
            ExamSessionId = sessionId,
            QuestionId = questionId,
            FlaggedAt = DateTime.UtcNow
        };

        _db.ExamSessionQuestionFlags.Add(flag);

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnflagQuestionAsync(
        Guid sessionId,
        Guid questionId)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            throw new KeyNotFoundException(
                "The specified exam session was not found.");
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Questions can only be unflagged while the exam session is in progress.");
        }

        var flag = await _db.ExamSessionQuestionFlags
            .FirstOrDefaultAsync(f =>
                f.ExamSessionId == sessionId &&
                f.QuestionId == questionId);

        if (flag is null)
        {
            return false;
        }

        _db.ExamSessionQuestionFlags.Remove(flag);

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<ExamSessionQuestionStatusResponse>?> GetFlaggedQuestionsAsync(
        Guid sessionId)
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
                "Flagged questions can only be retrieved while the exam session is in progress.");
        }

        var flaggedQuestions = await _db.ExamSessionQuestionFlags
            .AsNoTracking()
            .Where(f => f.ExamSessionId == sessionId)
            .Select(f => f.QuestionId)
            .ToListAsync();

        var positions = await _db.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamId == session.ExamId)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .Select(eq => eq.QuestionId)
            .ToListAsync();

        var result = new List<ExamSessionQuestionStatusResponse>();

        for (var i = 0; i < positions.Count; i++)
        {
            if (flaggedQuestions.Contains(positions[i]))
            {
                result.Add(new ExamSessionQuestionStatusResponse
                {
                    QuestionId = positions[i],
                    Position = i + 1,
                    IsAnswered = await _db.ExamSessionResponses.AnyAsync(r =>
                        r.ExamSessionId == sessionId &&
                        r.QuestionId == positions[i])
                });
            }
        }

        return result;
    }
}