using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionReviewService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionReviewService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionReviewResponse?> GetReviewAsync(
        Guid sessionId)
    {
        var session = await _db.ExamSessions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            return null;
        }

        if (session.Status != ExamSessionStatus.Completed)
        {
            throw new InvalidOperationException(
                "Question review is only available after the exam session has been completed.");
        }

        var questions = await _db.ExamQuestions
            .Where(eq => eq.ExamId == session.ExamId)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.Options)
            .OrderBy(eq => eq.QuestionId)
            .ToListAsync();

        var responses = await _db.ExamSessionResponses
            .Where(r => r.ExamSessionId == sessionId)
            .ToListAsync();

        var responseByQuestionId = responses
            .ToDictionary(r => r.QuestionId);

        var reviewQuestions = questions.Select(eq =>
        {
            var question = eq.Question;

            responseByQuestionId.TryGetValue(
                question.Id,
                out var response);

            var selectedOption = response is null
                ? null
                : question.Options
                    .FirstOrDefault(o => o.Id == response.SelectedOptionId);

            var correctOption = question.Options
                .FirstOrDefault(o => o.IsCorrect);

            return new ExamSessionReviewQuestionResponse
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                Difficulty = question.Difficulty.ToString(),

                SelectedOption = selectedOption is null
                    ? null
                    : new ExamSessionReviewOptionResponse
                    {
                        Id = selectedOption.Id,
                        Text = selectedOption.Text
                    },

                CorrectOption = correctOption is null
                    ? null
                    : new ExamSessionReviewOptionResponse
                    {
                        Id = correctOption.Id,
                        Text = correctOption.Text
                    },

                IsCorrect = response?.IsCorrect,
                Rationale = question.Rationale
            };
        }).ToList();

        return new ExamSessionReviewResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            Questions = reviewQuestions
        };
    }
}