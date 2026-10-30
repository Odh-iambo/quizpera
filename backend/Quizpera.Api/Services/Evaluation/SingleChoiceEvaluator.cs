using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class SingleChoiceEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public SingleChoiceEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.SingleChoice;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        if (!request.SelectedOptionId.HasValue)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.SingleChoice);

        if (question is null)
        {
            return null;
        }

        if (question.ScoringMethod is not QuestionScoringMethod.AllOrNothing
            and not QuestionScoringMethod.RationaleScoring)
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

        var result = new QuestionEvaluationResult
        {
            QuestionId = request.QuestionId,
            SelectedOptionId = selectedOptionId,
            IsCorrect = option.IsCorrect,
            MaximumPoints = question.MaximumPoints,
            PointsEarned = option.IsCorrect
                ? question.MaximumPoints
                : 0m
        };

        if (question.ScoringMethod == QuestionScoringMethod.RationaleScoring)
        {
            var selectedRationaleIds =
                request.SelectedRationaleOptionIds ?? [];

            if (selectedRationaleIds.Count == 0)
            {
                return null;
            }

            // Reject duplicate rationale selections.
            if (selectedRationaleIds.Distinct().Count()
                != selectedRationaleIds.Count)
            {
                return null;
            }

            var rationaleOptions = await _db.QuestionRationaleOptions
                .Where(r =>
                    r.QuestionId == request.QuestionId &&
                    selectedRationaleIds.Contains(r.Id))
                .ToListAsync();

            // Every selected rationale must belong to this question.
            if (rationaleOptions.Count != selectedRationaleIds.Count)
            {
                return null;
            }

            result.SelectedRationaleOptionIds =
                selectedRationaleIds.ToList();

            // Rationale scoring divides the available points equally
            // between the primary answer and the rationale.
            var pointsPerComponent = question.MaximumPoints / 2m;

            result.PointsEarned =
                (option.IsCorrect ? pointsPerComponent : 0m) +
                (rationaleOptions.All(r => r.IsCorrect)
                    ? pointsPerComponent
                    : 0m);
        }
        else if (request.SelectedRationaleOptionIds is { Count: > 0 })
        {
            // Rationale selections are only accepted when the question
            // is configured for rationale scoring.
            return null;
        }

        return result;
    }
}