using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class OrderedResponseEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public OrderedResponseEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.OrderedResponse;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        var submittedOptionIds = request.OrderedOptionIds;

        if (submittedOptionIds is null || submittedOptionIds.Count == 0)
        {
            return null;
        }

        if (submittedOptionIds.Distinct().Count() != submittedOptionIds.Count)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.OrderedResponse);

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

        var options = await _db.QuestionOptions
            .Where(option => option.QuestionId == request.QuestionId)
            .ToListAsync();

        if (options.Count < 2 ||
            options.Any(option => option.CorrectOrder is null))
        {
            return null;
        }

        if (options.Any(option => option.CorrectOrder < 1) ||
            options.Select(option => option.CorrectOrder)
                .Distinct()
                .Count() != options.Count ||
            !options.Select(option => option.CorrectOrder!.Value)
                .OrderBy(order => order)
                .SequenceEqual(Enumerable.Range(1, options.Count)))
        {
            return null;
        }

        var availableOptionIds = options
            .Select(option => option.Id)
            .ToHashSet();

        if (submittedOptionIds.Any(id => !availableOptionIds.Contains(id)))
        {
            return null;
        }

        var correctOptionIds = options
            .OrderBy(option => option.CorrectOrder)
            .Select(option => option.Id)
            .ToList();

        var isCorrect = submittedOptionIds.SequenceEqual(correctOptionIds);

        return new QuestionEvaluationResult
        {
            QuestionId = request.QuestionId,
            SelectedOptionId = submittedOptionIds[0],
            SelectedOptionIds = submittedOptionIds.ToList(),
            IsCorrect = isCorrect,
            PointsEarned = isCorrect
                ? question.MaximumPoints
                : 0m,
            MaximumPoints = question.MaximumPoints
        };
    }
}