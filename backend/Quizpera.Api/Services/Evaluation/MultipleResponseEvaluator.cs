
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class MultipleResponseEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public MultipleResponseEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.MultipleResponse;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        var selectedOptionIds = request.SelectedOptionIds;

        // At least one option must be selected.
        if (selectedOptionIds.Count == 0)
        {
            return null;
        }

        // Reject duplicate selections.
        var selectedIds = selectedOptionIds.ToHashSet();

        if (selectedIds.Count != selectedOptionIds.Count)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.MultipleResponse);

        if (question is null)
        {
            return null;
        }

        var questionOptions = await _db.QuestionOptions
            .Where(option => option.QuestionId == request.QuestionId)
            .ToListAsync();

        var availableIds = questionOptions
            .Select(option => option.Id)
            .ToHashSet();

        // Reject options belonging to another question.
        if (!selectedIds.IsSubsetOf(availableIds))
        {
            return null;
        }

        var correctIds = questionOptions
            .Where(option => option.IsCorrect)
            .Select(option => option.Id)
            .ToHashSet();

       var isCorrect =
    correctIds.Count > 0 &&
    selectedIds.SetEquals(correctIds);

var pointsEarned = question.ScoringMethod switch
{
    QuestionScoringMethod.AllOrNothing =>
        isCorrect ? question.MaximumPoints : 0m,

    QuestionScoringMethod.ZeroOne =>
        CalculateZeroOneScore(
            selectedIds,
            correctIds,
            question.MaximumPoints),

    QuestionScoringMethod.PlusMinus =>
        CalculatePlusMinusScore(
            selectedIds,
            correctIds,
            question.MaximumPoints),

    _ => throw new NotSupportedException(
        $"Scoring method '{question.ScoringMethod}' is not supported " +
        $"for question type '{question.Type}'.")
};

return new QuestionEvaluationResult
{
    QuestionId = request.QuestionId,
    SelectedOptionId = selectedIds.Count == 1
        ? selectedIds.First()
        : Guid.Empty,
    SelectedOptionIds = selectedOptionIds.ToList(),
    IsCorrect = isCorrect,
    PointsEarned = pointsEarned,
    MaximumPoints = question.MaximumPoints
};
    }

    private static decimal CalculateZeroOneScore(
    HashSet<Guid> selectedIds,
    HashSet<Guid> correctIds,
    decimal maximumPoints)
{
    if (correctIds.Count == 0 || maximumPoints <= 0m)
    {
        return 0m;
    }

    var correctSelections = selectedIds.Count(
        id => correctIds.Contains(id));

    // Guarantee that a fully correct response earns the exact maximum.
    if (correctSelections == correctIds.Count)
    {
        return maximumPoints;
    }

    var pointsPerOption = maximumPoints / correctIds.Count;

    var score = correctSelections * pointsPerOption;

    return Math.Clamp(score, 0m, maximumPoints);
}


    private static decimal CalculatePlusMinusScore(
    HashSet<Guid> selectedIds,
    HashSet<Guid> correctIds,
    decimal maximumPoints)
{
    if (correctIds.Count == 0 || maximumPoints <= 0m)
    {
        return 0m;
    }

    var correctSelections = selectedIds.Count(id => correctIds.Contains(id));
    var incorrectSelections = selectedIds.Count - correctSelections;

    var pointsPerOption = maximumPoints / correctIds.Count;

    var rawScore =
        (correctSelections * pointsPerOption) -
        (incorrectSelections * pointsPerOption);

    return Math.Clamp(rawScore, 0m, maximumPoints);
}
}