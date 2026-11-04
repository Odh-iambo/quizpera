using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class MatrixGridEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public MatrixGridEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.MatrixGrid;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        var question = await _db.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == request.QuestionId &&
                q.Type == QuestionType.MatrixGrid);

        if (question is null)
        {
            return null;
        }

        var rows = await _db.QuestionMatrixRows
            .Where(row => row.QuestionId == question.Id)
            .ToListAsync();

        var columns = await _db.QuestionMatrixColumns
            .Where(column => column.QuestionId == question.Id)
            .ToListAsync();

        var correctAnswers = await _db.QuestionMatrixAnswers
            .Where(answer => answer.QuestionId == question.Id)
            .ToListAsync();

        // A valid MatrixGrid requires rows, columns and one correct
        // mapping for every row.
        if (rows.Count < 2 ||
            columns.Count < 2 ||
            correctAnswers.Count != rows.Count)
        {
            return null;
        }

        var submittedAnswers = request.MatrixAnswers;

        // Require exactly one selection for every row.
        if (submittedAnswers.Count != rows.Count)
        {
            return null;
        }

        var rowIds = rows
            .Select(row => row.Id)
            .ToHashSet();

        var columnIds = columns
            .Select(column => column.Id)
            .ToHashSet();

        var correctMappings = correctAnswers
            .ToDictionary(
                answer => answer.MatrixRowId,
                answer => answer.CorrectColumnId);

        // Reject duplicate row submissions.
        var submittedRowIds = submittedAnswers
            .Select(answer => answer.MatrixRowId)
            .ToHashSet();

        if (submittedRowIds.Count != submittedAnswers.Count)
        {
            return null;
        }

        // Reject rows or columns belonging to another question.
        if (!submittedRowIds.SetEquals(rowIds) ||
            submittedAnswers.Any(answer =>
                !columnIds.Contains(answer.SelectedColumnId)))
        {
            return null;
        }

        // Every correct mapping must reference an existing row
        // and column belonging to this question.
        if (correctMappings.Count != rows.Count ||
            correctMappings.Any(mapping =>
                !rowIds.Contains(mapping.Key) ||
                !columnIds.Contains(mapping.Value)))
        {
            return null;
        }

        var correctSelections = submittedAnswers.Count(answer =>
            correctMappings[answer.MatrixRowId] ==
            answer.SelectedColumnId);

        var isCorrect = correctSelections == rows.Count;

        var pointsEarned = question.ScoringMethod switch
        {
            QuestionScoringMethod.AllOrNothing =>
                isCorrect ? question.MaximumPoints : 0m,

            QuestionScoringMethod.ZeroOne =>
                question.MaximumPoints * correctSelections / rows.Count,

            _ => throw new NotSupportedException(
                $"Scoring method '{question.ScoringMethod}' is not supported " +
                $"for question type '{question.Type}'.")
        };

        return new QuestionEvaluationResult
        {
            QuestionId = question.Id,
            MatrixAnswers = submittedAnswers.ToList(),
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            MaximumPoints = question.MaximumPoints
        };
    }
}