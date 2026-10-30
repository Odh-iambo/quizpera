using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ExamSessionQuestionService
{
    private readonly QuizperaDbContext _db;
    private readonly ClinicalCaseProgressionService _caseProgression;

    public ExamSessionQuestionService(QuizperaDbContext db)
    {
        _db = db;
        _caseProgression = new ClinicalCaseProgressionService(db);
    }

    public async Task<ExamSessionQuestionsResponse?> GetQuestionsAsync(
        Guid sessionId)
    {
        var session = await _db.ExamSessions
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

        var examQuestions = await _db.ExamQuestions
            .Where(eq => eq.ExamId == session.ExamId)
            .Include(eq => eq.Question)
                .ThenInclude(q => q.Options)
            .OrderBy(eq => eq.DisplayOrder)
            .ThenBy(eq => eq.QuestionId)
            .ToListAsync();

        // Only include questions that the student can currently access.
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

        var savedResponses = await _db.ExamSessionResponses
            .Where(r => r.ExamSessionId == sessionId)
            .Include(r => r.SelectedOptions)
            .ToDictionaryAsync(r => r.QuestionId);

        var questions = accessibleQuestions
            .Select(eq =>
            {
                savedResponses.TryGetValue(
                    eq.QuestionId,
                    out var savedResponse);

                return new ExamSessionQuestionResponse
                {
                    QuestionId = eq.QuestionId,
                    DisplayOrder = eq.DisplayOrder,
                    Type = eq.Question.Type.ToString(),
                    Stem = eq.Question.Stem,
                    Difficulty = eq.Question.Difficulty.ToString(),

                    Options = eq.Question.Options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new ExamSessionQuestionOptionResponse
                        {
                            Id = o.Id,
                            Text = o.Text,
                            DisplayOrder = o.DisplayOrder
                        })
                        .ToList(),

                    SavedAnswer = MapSavedAnswer(savedResponse)
                };
            })
            .ToList();

        return new ExamSessionQuestionsResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            Questions = questions
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

        // Single-choice responses may use SelectedOptionId without
        // having a corresponding SelectedOptions child record.
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