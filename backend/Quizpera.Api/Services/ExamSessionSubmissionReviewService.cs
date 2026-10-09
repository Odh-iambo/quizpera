using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionSubmissionReviewService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionSubmissionReviewService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionSubmissionReviewResponse?> GetReviewAsync(
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
                "Submission review is only available while the exam session is in progress.");
        }

        var examQuestions = await _db.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamId == session.ExamId)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .Select(eq => eq.QuestionId)
            .ToListAsync();

        var answeredQuestionIds = await _db.ExamSessionResponses
            .AsNoTracking()
            .Where(r => r.ExamSessionId == sessionId)
            .Select(r => r.QuestionId)
            .ToListAsync();

        var flaggedQuestionIds = await _db.ExamSessionQuestionFlags
            .AsNoTracking()
            .Where(f => f.ExamSessionId == sessionId)
            .Select(f => f.QuestionId)
            .ToListAsync();

        var answeredSet = answeredQuestionIds.ToHashSet();
        var flaggedSet = flaggedQuestionIds.ToHashSet();

        var questions = examQuestions
            .Select((questionId, index) =>
                new ExamSessionSubmissionReviewQuestionResponse
                {
                    QuestionId = questionId,
                    Position = index + 1,
                    IsAnswered = answeredSet.Contains(questionId),
                    IsFlagged = flaggedSet.Contains(questionId)
                })
            .ToList();

        var answeredCount = questions.Count(q => q.IsAnswered);
        var flaggedCount = questions.Count(q => q.IsFlagged);
        var totalCount = questions.Count;

        return new ExamSessionSubmissionReviewResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            TotalQuestions = totalCount,
            AnsweredQuestions = answeredCount,
            UnansweredQuestions = totalCount - answeredCount,
            FlaggedQuestions = flaggedCount,
            HasUnansweredQuestions = answeredCount < totalCount,
            Questions = questions
        };
    }
}