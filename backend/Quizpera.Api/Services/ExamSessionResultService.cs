using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionResultService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionResultService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionResultResponse?> GetResultAsync(
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

        var responses = await _db.ExamSessionResponses
            .Where(r => r.ExamSessionId == sessionId)
            .ToListAsync();

        var answeredQuestions = responses.Count;
        var correctAnswers = responses.Count(r => r.IsCorrect);
        var incorrectAnswers = answeredQuestions - correctAnswers;
        var unansweredQuestions = totalQuestions - answeredQuestions;

        decimal? scorePercentage = null;

        if (answeredQuestions > 0)
        {
            scorePercentage =
                Math.Round(
                    (decimal)correctAnswers / answeredQuestions * 100,
                    2);
        }

        return new ExamSessionResultResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredQuestions,
            CorrectAnswers = correctAnswers,
            IncorrectAnswers = incorrectAnswers,
            UnansweredQuestions = unansweredQuestions,
            ScorePercentage = scorePercentage,
        

            
        };
    }
}