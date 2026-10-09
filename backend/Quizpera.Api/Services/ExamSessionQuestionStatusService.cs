using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionQuestionStatusService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionQuestionStatusService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExamSessionQuestionStatusResponse>?> GetQuestionStatusesAsync(
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
                "Question statuses can only be retrieved from an exam session that is in progress.");
        }

        var statuses = await _db.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamId == session.ExamId)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .Select(eq => new ExamSessionQuestionStatusResponse
            {
                QuestionId = eq.QuestionId,
                Position = 0,
                IsAnswered = _db.ExamSessionResponses.Any(r =>
                    r.ExamSessionId == sessionId &&
                    r.QuestionId == eq.QuestionId)
            })
            .ToListAsync();

        for (var i = 0; i < statuses.Count; i++)
        {
            statuses[i].Position = i + 1;
        }

        return statuses;
    }
}