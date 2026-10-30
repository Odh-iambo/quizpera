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

        // Load exam questions, answer options, and rationale options.
        var questions = await _db.ExamQuestions
            .Where(eq => eq.ExamId == session.ExamId)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.Options)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.RationaleOptions)
            .OrderBy(eq => eq.QuestionId)
            .ToListAsync();

        // Load submitted responses and their selected rationale options.
        var responses = await _db.ExamSessionResponses
            .Where(r => r.ExamSessionId == sessionId)
            .Include(r => r.SelectedRationaleOptions)
                .ThenInclude(ro => ro.QuestionRationaleOption)
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

            var selectedRationaleOptions = response is null
                ? []
                : response.SelectedRationaleOptions
                    .OrderBy(ro => ro.SelectedOrder ?? int.MaxValue)
                    .Select(ro => new ExamSessionReviewRationaleOptionResponse
                    {
                        Id = ro.QuestionRationaleOption.Id,
                        Text = ro.QuestionRationaleOption.Text,
                        DisplayOrder = ro.QuestionRationaleOption.DisplayOrder,
                        IsCorrect = ro.QuestionRationaleOption.IsCorrect
                    })
                    .ToList();

            var correctRationaleOptions = question.RationaleOptions
                .Where(ro => ro.IsCorrect)
                .OrderBy(ro => ro.DisplayOrder)
                .Select(ro => new ExamSessionReviewRationaleOptionResponse
                {
                    Id = ro.Id,
                    Text = ro.Text,
                    DisplayOrder = ro.DisplayOrder,
                    IsCorrect = ro.IsCorrect
                })
                .ToList();

            return new ExamSessionReviewQuestionResponse
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                Type = question.Type.ToString(),
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

                SelectedRationaleOptions = selectedRationaleOptions,
                CorrectRationaleOptions = correctRationaleOptions,

                IsCorrect = response?.IsCorrect,
                Rationale = question.Rationale,

                NumericAnswer = response?.NumericAnswer,
                CorrectNumericAnswer = question.NumericCorrectAnswer,

                PointsEarned = response?.PointsEarned,
                MaximumPoints = response?.MaximumPoints
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
