using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class TrueFalseEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public TrueFalseEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.TrueFalse;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        // A True/False answer must identify the selected option.
        if (!request.SelectedOptionId.HasValue)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.TrueFalse);

        if (question is null)
        {
            return null;
        }

        if (question.ScoringMethod != QuestionScoringMethod.AllOrNothing)
        {
            throw new NotSupportedException(
                $"Scoring method '{question.ScoringMethod}' is not supported " +
                $"for question type '{question.Type}'.");
        }

        var selectedOptionId = request.SelectedOptionId.Value;

        var option = await _db.QuestionOptions
            .FirstOrDefaultAsync(o =>
                o.Id == selectedOptionId &&
                o.QuestionId == request.QuestionId);

        if (option is null)
        {
            return null;
        }

        return new QuestionEvaluationResult
        {
            QuestionId = request.QuestionId,
            SelectedOptionId = selectedOptionId,
            IsCorrect = option.IsCorrect,
            PointsEarned = option.IsCorrect
                ? question.MaximumPoints
                : 0m,
            MaximumPoints = question.MaximumPoints
        };
    }
}