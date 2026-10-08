using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionProgressService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionProgressService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionProgressResponse?> GetProgressAsync(
        Guid sessionId)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            return null;
        }

        var totalQuestions = await _db.ExamQuestions
            .CountAsync(eq => eq.ExamId == session.ExamId);

        var answeredQuestions = await _db.ExamSessionResponses
            .CountAsync(r => r.ExamSessionId == sessionId);

        var unansweredQuestions =
            totalQuestions - answeredQuestions;

        decimal progressPercentage = 0;

        if (totalQuestions > 0)
        {
            progressPercentage =
                Math.Round(
                    (decimal)answeredQuestions / totalQuestions * 100,
                    2);
        }

        return new ExamSessionProgressResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredQuestions,
            UnansweredQuestions = unansweredQuestions,
            ProgressPercentage = progressPercentage
        };
    }
}