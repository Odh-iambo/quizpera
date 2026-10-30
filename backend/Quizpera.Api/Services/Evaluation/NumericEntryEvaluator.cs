using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class NumericEntryEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public NumericEntryEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.NumericEntry;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        if (!request.NumericAnswer.HasValue)
        {
            return null;
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.NumericEntry);

        if (question is null ||
            !question.NumericCorrectAnswer.HasValue)
        {
            return null;
        }

        if (question.ScoringMethod != QuestionScoringMethod.AllOrNothing)
        {
            throw new NotSupportedException(
                $"Scoring method '{question.ScoringMethod}' is not supported " +
                $"for question type '{question.Type}'.");
        }

        var tolerance = question.NumericTolerance ?? 0m;

        var difference = Math.Abs(
            request.NumericAnswer.Value -
            question.NumericCorrectAnswer.Value);

        var isCorrect = difference <= tolerance;

        return new QuestionEvaluationResult
        {
            QuestionId = request.QuestionId,
            NumericAnswer = request.NumericAnswer.Value,
            IsCorrect = isCorrect,
            PointsEarned = isCorrect
                ? question.MaximumPoints
                : 0m,
            MaximumPoints = question.MaximumPoints
        };
    }
}
