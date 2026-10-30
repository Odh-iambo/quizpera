using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionNavigationService
{
    private readonly QuizperaDbContext _db;
    private readonly ClinicalCaseProgressionService _caseProgression;

    public ExamSessionNavigationService(QuizperaDbContext db)
    {
        _db = db;
        _caseProgression = new ClinicalCaseProgressionService(db);
    }

    public async Task<ExamSessionNavigationResponse?> GetQuestionAsync(
        Guid sessionId,
        int position)
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
                "Questions can only be retrieved from an exam session that is in progress.");
        }

        if (position < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Question position must be at least 1.");
        }

        var examQuestions = await _db.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamId == session.ExamId)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.Options)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .ToListAsync();

        // Filter before calculating the total and selecting a position.
        // This prevents locked case questions from affecting navigation.
        var accessibleQuestions = new List<ExamQuestion>();

        foreach (var examQuestion in examQuestions)
        {
            if (await _caseProgression.CanAccessQuestionAsync(
                    sessionId,
                    examQuestion.QuestionId))
            {
                accessibleQuestions.Add(examQuestion);
            }
        }

        var totalQuestions = accessibleQuestions.Count;

        if (position > totalQuestions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Question position must be between 1 and {totalQuestions}.");
        }

        var examQuestionAtPosition = accessibleQuestions[position - 1];

        var savedResponse = await _db.ExamSessionResponses
            .AsNoTracking()
            .Include(r => r.SelectedOptions)
            .FirstOrDefaultAsync(r =>
                r.ExamSessionId == sessionId &&
                r.QuestionId == examQuestionAtPosition.QuestionId);

        var question = examQuestionAtPosition.Question;

        return new ExamSessionNavigationResponse
        {
            SessionId = session.Id,
            CurrentPosition = position,
            TotalQuestions = totalQuestions,
            IsAnswered = savedResponse is not null,

            Question = new ExamSessionQuestionResponse
            {
                QuestionId = examQuestionAtPosition.QuestionId,
                DisplayOrder = examQuestionAtPosition.DisplayOrder,
                Type = question.Type.ToString(),
                Stem = question.Stem,
                Difficulty = question.Difficulty.ToString(),

                Options = question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new ExamSessionQuestionOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList(),

                SavedAnswer = MapSavedAnswer(savedResponse)
            }
        };
    }

    private static ExamSessionSavedAnswerResponse? MapSavedAnswer(
        ExamSessionResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        var selectedOptions = response.SelectedOptions
            .OrderBy(o => o.SelectedOrder ?? int.MaxValue)
            .ThenBy(o => o.QuestionOptionId)
            .ToList();

        var selectedOptionIds = selectedOptions
            .Select(o => o.QuestionOptionId)
            .ToList();

        if (response.SelectedOptionId.HasValue &&
            !selectedOptionIds.Contains(response.SelectedOptionId.Value))
        {
            selectedOptionIds.Insert(
                0,
                response.SelectedOptionId.Value);
        }

        return new ExamSessionSavedAnswerResponse
        {
            SelectedOptionId = response.SelectedOptionId,
            SelectedOptionIds = selectedOptionIds,
            SelectedOrders = selectedOptions
                .Select(o => o.SelectedOrder)
                .ToList(),
            NumericAnswer = response.NumericAnswer,
            AnsweredAt = response.AnsweredAt
        };
    }
}