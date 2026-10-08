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
        SubmitExamAnswerRequest request)
    {
        var session = await _db.ExamSessions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

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

        var selectedOption = await _db.QuestionOptions
            .FirstOrDefaultAsync(option =>
                option.Id == request.SelectedOptionId &&
                option.QuestionId == request.QuestionId);

        if (selectedOption is null)
        {
            throw new InvalidOperationException(
                "The selected option does not belong to the specified question.");
        }

        var question = await _db.Questions
            .FirstAsync(q => q.Id == request.QuestionId);

        var evaluation = await _evaluationService.EvaluateAsync(
            new StudentAnswerRequest
            {
                QuestionId = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId
            },
            question.Type);

        if (evaluation is null)
        {
            throw new InvalidOperationException(
                "The question type is not supported by the evaluation system.");
        }

        var response = await _db.ExamSessionResponses
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

        response.SelectedOptionId = request.SelectedOptionId;
        response.IsCorrect = evaluation.IsCorrect;
        response.AnsweredAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response;
    }

    public async Task<ExamSession?> CompleteSessionAsync(Guid sessionId)
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
            "Only an exam session that is in progress can be completed.");
    }

    session.Status = ExamSessionStatus.Completed;
    session.CompletedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();

    return session;
}
}