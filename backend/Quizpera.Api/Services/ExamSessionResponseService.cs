using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services.Evaluation;

namespace Quizpera.Api.Services;

public class ExamSessionResponseService
{
    private readonly QuizperaDbContext _db;
    private readonly QuestionEvaluationService _evaluationService;

    public ExamSessionResponseService(
        QuizperaDbContext db,
        QuestionEvaluationService evaluationService)
    {
        _db = db;
        _evaluationService = evaluationService;
    }

    public async Task<ExamSessionResponse?> SubmitAnswerAsync(
        Guid sessionId,
        string userId,
        SubmitExamAnswerRequest request)
    {
        var session = await _db.ExamSessions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
        {
            return null;
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Answers can only be submitted to an exam session that is in progress.");
        }

        var examQuestionExists = await _db.ExamQuestions
            .AnyAsync(eq =>
                eq.ExamId == session.ExamId &&
                eq.QuestionId == request.QuestionId);

        if (!examQuestionExists)
        {
            throw new InvalidOperationException(
                "The specified question does not belong to this exam.");
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId);

        if (question is null)
        {
            throw new InvalidOperationException(
                "The specified question was not found.");
        }

        if (request.SelectedOptionId.HasValue)
        {
            var selectedOptionExists = await _db.QuestionOptions
                .AnyAsync(option =>
                    option.Id == request.SelectedOptionId.Value &&
                    option.QuestionId == request.QuestionId);

            if (!selectedOptionExists)
            {
                throw new InvalidOperationException(
                    "The selected option does not belong to the specified question.");
            }
        }
        else if (question.Type == QuestionType.SingleChoice ||
                 question.Type == QuestionType.TrueFalse)
        {
            throw new InvalidOperationException(
                "A selected option is required for this question type.");
        }

        var evaluation = await _evaluationService.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId,
                SelectedOptionIds = request.SelectedOptionIds,
                OrderedOptionIds = request.OrderedOptionIds,
                NumericAnswer = request.NumericAnswer,
                SelectedRationaleOptionIds = request.SelectedRationaleOptionIds
            },
            question.Type);

        if (evaluation is null)
        {
            throw new InvalidOperationException(
                "The question type is not supported by the evaluation system.");
        }

        var response = await _db.ExamSessionResponses
            .Include(r => r.SelectedRationaleOptions)
            .FirstOrDefaultAsync(r =>
                r.ExamSessionId == sessionId &&
                r.QuestionId == request.QuestionId);

        if (response is null)
        {
            response = new ExamSessionResponse
            {
                Id = Guid.NewGuid(),
                ExamSessionId = sessionId,
                QuestionId = request.QuestionId
            };

            _db.ExamSessionResponses.Add(response);
        }

        response.SelectedOptionId =
            question.Type == QuestionType.NumericEntry
                ? null
                : request.SelectedOptionId;

        response.NumericAnswer =
            question.Type == QuestionType.NumericEntry
                ? evaluation.NumericAnswer
                : null;

        response.IsCorrect = evaluation.IsCorrect;
        response.PointsEarned = evaluation.PointsEarned;
        response.MaximumPoints = evaluation.MaximumPoints;
        response.AnsweredAt = DateTime.UtcNow;

        // Replace previous rationale selections with the latest submission.
        response.SelectedRationaleOptions.Clear();

        foreach (var rationaleOptionId in evaluation.SelectedRationaleOptionIds)
        {
            response.SelectedRationaleOptions.Add(
                new ExamSessionResponseRationaleOption
                {
                    ExamSessionResponseId = response.Id,
                    QuestionRationaleOptionId = rationaleOptionId
                });
        }

        await _db.SaveChangesAsync();

        return response;
    }

    public async Task<ExamSession?> CompleteSessionAsync(
        Guid sessionId,
        string userId)
    {
        var session = await _db.ExamSessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.UserId == userId);

        if (session is null)
        {
            return null;
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Only an exam session that is in progress can be completed.");
        }

        session.Status = ExamSessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return session;
    }
}