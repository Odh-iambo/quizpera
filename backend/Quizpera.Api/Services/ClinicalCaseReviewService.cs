using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services;

public class ClinicalCaseReviewService
{
    private readonly QuizperaDbContext _db;

    public ClinicalCaseReviewService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<ExamSessionReviewResponse?> GetReviewAsync(
        Guid sessionId,
        Guid caseId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.Status,
                s.ExamId,
                ExamProgramId = s.Exam.ExamProgramId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return null;

        var clinicalCase = await _db.ClinicalCases
            .AsNoTracking()
            .Where(c =>
                c.Id == caseId &&
                c.IsPublished &&
                c.ExamProgramId == session.ExamProgramId)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (clinicalCase is null)
            return null;


            // Verify that this clinical case actually belongs to the exam.
// A case sharing the exam's program is not sufficient.
var caseQuestionIds = await _db.ClinicalCaseQuestions
    .AsNoTracking()
    .Where(cq => cq.ClinicalCaseId == caseId)
    .Select(cq => cq.QuestionId)
    .Distinct()
    .ToListAsync(cancellationToken);

if (caseQuestionIds.Count == 0)
{
    return null;
}

var assignedQuestionCount = await _db.ExamQuestions
    .AsNoTracking()
    .Where(eq =>
        eq.ExamId == session.ExamId &&
        caseQuestionIds.Contains(eq.QuestionId))
    .Select(eq => eq.QuestionId)
    .Distinct()
    .CountAsync(cancellationToken);

// The case has no questions in this exam.
if (assignedQuestionCount == 0)
{
    return null;
}

// Never return a partial review for an inconsistently configured case.
if (assignedQuestionCount != caseQuestionIds.Count)
{
    throw new InvalidOperationException(
        "Clinical case review is unavailable because the case is only " +
        "partially included in this exam.");
}

        var progress = await _db.ExamSessionCaseProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ExamSessionId == sessionId &&
                     p.ClinicalCaseId == caseId,
                cancellationToken);

        if (progress is null)
            return null;

        if (!progress.IsCompleted)
        {
            throw new InvalidOperationException(
                "Clinical case review is only available after the case has been completed.");
        }

        var questions = await _db.ClinicalCaseQuestions
            .AsNoTracking()
            .Where(cq => cq.ClinicalCaseId == caseId)
            .Include(cq => cq.Question)
                .ThenInclude(q => q.Options)
            .OrderBy(cq => cq.DisplayOrder)
            .ToListAsync(cancellationToken);

        var questionIds = questions
            .Select(cq => cq.QuestionId)
            .ToList();

        var responses = await _db.ExamSessionResponses
            .Where(r =>
                r.ExamSessionId == sessionId &&
                questionIds.Contains(r.QuestionId))
            .Include(r => r.SelectedOptions)
            .ToListAsync(cancellationToken);

        var responseByQuestionId = responses
            .ToDictionary(r => r.QuestionId);

        var reviewQuestions = questions.Select(cq =>
        {
            var question = cq.Question;

            responseByQuestionId.TryGetValue(
                question.Id,
                out var response);

            var selectedOptionIds = response is null
                ? new List<Guid>()
                : response.SelectedOptions
                    .OrderBy(ro => ro.SelectedOrder ?? int.MaxValue)
                    .Select(ro => ro.QuestionOptionId)
                    .ToList();

                if (response?.SelectedOptionId is Guid selectedOptionId &&
    selectedOptionIds.Count == 0)
{
    selectedOptionIds.Add(selectedOptionId);
}

            var selectedOptions = selectedOptionIds
                .Select(id => question.Options.FirstOrDefault(o => o.Id == id))
                .Where(o => o is not null)
                .Select(o => new ExamSessionReviewOptionResponse
                {
                    Id = o!.Id,
                    Text = o.Text
                })
                .ToList();

            var correctOptions = question.Type == QuestionType.OrderedResponse
                ? question.Options
                    .OrderBy(o => o.CorrectOrder)
                    .Select(o => new ExamSessionReviewOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        CorrectOrder = o.CorrectOrder
                    })
                    .ToList()
                : question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => new ExamSessionReviewOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text
                    })
                    .ToList();

            return new ExamSessionReviewQuestionResponse
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                Difficulty = question.Difficulty.ToString(),
                Type = question.Type.ToString(),
                SelectedOption = selectedOptions.FirstOrDefault(),
                CorrectOption = correctOptions.FirstOrDefault(),
                SelectedOptions = selectedOptions,
                CorrectOptions = correctOptions,
                IsCorrect = response?.IsCorrect,
                Rationale = question.Rationale
            };
        }).ToList();

        return new ExamSessionReviewResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            Questions = reviewQuestions
        };
    }
}