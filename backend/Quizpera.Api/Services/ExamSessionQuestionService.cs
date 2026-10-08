using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionQuestionService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionQuestionService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionQuestionsResponse?> GetQuestionsAsync(
        Guid sessionId)
    {
        var session = await _db.ExamSessions
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

        var examQuestions = await _db.ExamQuestions
            .Where(eq => eq.ExamId == session.ExamId)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.Options)
            .OrderBy(eq => eq.DisplayOrder)
            .ToListAsync();

        var questions = examQuestions
            .Select(eq => new ExamSessionQuestionResponse
            {
                QuestionId = eq.QuestionId,
                DisplayOrder = eq.DisplayOrder,
                Stem = eq.Question.Stem,
                Difficulty = eq.Question.Difficulty.ToString(),

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
            .ToList();

        return new ExamSessionQuestionsResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            Questions = questions
        };
    }
}