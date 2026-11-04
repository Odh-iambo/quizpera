using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class BowTieEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public BowTieEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.BowTie;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        // A valid Bow-Tie answer must contain exactly five selections.
        var submittedIds = request.BowTieOptionIds;

        if (submittedIds.Count != 5)
        {
            return null;
        }

        // Reject duplicate selections.
        var selectedIds = submittedIds.ToHashSet();

        if (selectedIds.Count != submittedIds.Count)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.BowTie);

        if (question is null)
        {
            return null;
        }

        var options = await _db.BowTieOptions
            .Where(option => option.QuestionId == question.Id)
            .ToListAsync();

        // Validate the question's answer-key configuration.
        var correctOptions = options
            .Where(option => option.IsCorrect)
            .ToList();

        if (correctOptions.Count(option =>
                option.Category == BowTieOptionCategory.Condition) != 1 ||
            correctOptions.Count(option =>
                option.Category == BowTieOptionCategory.Action) != 2 ||
            correctOptions.Count(option =>
                option.Category == BowTieOptionCategory.Parameter) != 2)
        {
            return null;
        }

        // Ensure the question has enough available options per category.
        if (options.Count(option =>
                option.Category == BowTieOptionCategory.Condition) < 1 ||
            options.Count(option =>
                option.Category == BowTieOptionCategory.Action) < 2 ||
            options.Count(option =>
                option.Category == BowTieOptionCategory.Parameter) < 2)
        {
            return null;
        }

        var availableIds = options
            .Select(option => option.Id)
            .ToHashSet();

        // Reject selections belonging to another question.
        if (!selectedIds.IsSubsetOf(availableIds))
        {
            return null;
        }

        var selectedOptions = options
            .Where(option => selectedIds.Contains(option.Id))
            .ToList();

        // Require exactly 1 Condition, 2 Actions and 2 Parameters.
        if (selectedOptions.Count(option =>
                option.Category == BowTieOptionCategory.Condition) != 1 ||
            selectedOptions.Count(option =>
                option.Category == BowTieOptionCategory.Action) != 2 ||
            selectedOptions.Count(option =>
                option.Category == BowTieOptionCategory.Parameter) != 2)
        {
            return null;
        }

        var correctIds = correctOptions
            .Select(option => option.Id)
            .ToHashSet();

        var correctSelections = selectedIds.Count(id =>
            correctIds.Contains(id));

        var isCorrect = correctSelections == 5;

        var pointsEarned = question.ScoringMethod switch
        {
            QuestionScoringMethod.AllOrNothing =>
                isCorrect ? question.MaximumPoints : 0m,

            QuestionScoringMethod.ZeroOne =>
                question.MaximumPoints * correctSelections / 5m,

            _ => throw new NotSupportedException(
                $"Scoring method '{question.ScoringMethod}' is not supported " +
                $"for question type '{question.Type}'.")
        };

        return new QuestionEvaluationResult
        {
            QuestionId = question.Id,
            BowTieOptionIds = submittedIds.ToList(),
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            MaximumPoints = question.MaximumPoints
        };
    }
}