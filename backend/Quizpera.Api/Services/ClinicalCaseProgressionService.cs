using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Contracts.Questions;

namespace Quizpera.Api.Services;

public class ClinicalCaseProgressionService
{
    private readonly QuizperaDbContext _db;

    public ClinicalCaseProgressionService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task InitializeSessionCasesAsync(
    Guid sessionId,
    Guid examId,
    CancellationToken cancellationToken = default)
{
    var exam = await _db.Exams
        .AsNoTracking()
        .Where(e => e.Id == examId)
        .Select(e => new
        {
            e.Id,
            e.ExamProgramId
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (exam is null)
    {
        throw new InvalidOperationException(
            "Cannot initialize clinical cases because the exam was not found.");
    }

    var session = await _db.ExamSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId)
        .Select(s => new
        {
            s.ExamId,
            s.Status
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (session is null || session.ExamId != examId)
    {
        throw new InvalidOperationException(
            "Cannot initialize clinical cases because the exam session " +
            "does not belong to the specified exam.");
    }

    if (session.Status != ExamSessionStatus.InProgress)
    {
        throw new InvalidOperationException(
            "Clinical cases can only be initialized for an in-progress exam session.");
    }

    var examQuestionIds = await _db.ExamQuestions
        .Where(eq => eq.ExamId == examId)
        .Select(eq => eq.QuestionId)
        .ToHashSetAsync(cancellationToken);

    var cases = await _db.ClinicalCases
        .Where(c =>
            c.ExamProgramId == exam.ExamProgramId &&
            c.IsPublished &&
            c.Questions.Any())
        .Include(c => c.Stages)
        .Include(c => c.Questions)
        .ToListAsync(cancellationToken);

    if (cases.Count == 0)
    {
        return;
    }

    var existingCaseIds = await _db.ExamSessionCaseProgress
        .Where(p => p.ExamSessionId == sessionId)
        .Select(p => p.ClinicalCaseId)
        .ToHashSetAsync(cancellationToken);

    var now = DateTime.UtcNow;
    var progressRecords = new List<ExamSessionCaseProgress>();

    foreach (var clinicalCase in cases)
    {
        // A case with no questions in this exam is unrelated to this exam.
        var assignedQuestionCount = clinicalCase.Questions
            .Count(q => examQuestionIds.Contains(q.QuestionId));

        if (assignedQuestionCount == 0)
        {
            continue;
        }

        // Do not initialize a case partially. Every question belonging to
        // a selected case must be included in this exam.
        if (assignedQuestionCount != clinicalCase.Questions.Count)
        {
            throw new InvalidOperationException(
                $"Clinical case '{clinicalCase.Title}' is only partially included " +
                "in this exam. All questions belonging to the case must be included.");
        }

        if (clinicalCase.Stages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Published clinical case '{clinicalCase.Title}' has no stages.");
        }

        if (existingCaseIds.Contains(clinicalCase.Id))
        {
            continue;
        }

        var firstStage = clinicalCase.Stages
            .OrderBy(s => s.DisplayOrder)
            .First();

        progressRecords.Add(new ExamSessionCaseProgress
        {
            ExamSessionId = sessionId,
            ClinicalCaseId = clinicalCase.Id,
            CurrentStageId = firstStage.Id,
            LastUpdatedAt = now
        });
    }

    if (progressRecords.Count == 0)
    {
        return;
    }

    _db.ExamSessionCaseProgress.AddRange(progressRecords);

    await _db.SaveChangesAsync(cancellationToken);
}

    public async Task<bool> CanAccessQuestionAsync(
        Guid sessionId,
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var caseQuestions = await _db.ClinicalCaseQuestions
            .AsNoTracking()
            .Where(cq => cq.QuestionId == questionId)
            .Select(cq => new
            {
                cq.ClinicalCaseId,
                cq.ClinicalCaseStageId
            })
            .ToListAsync(cancellationToken);

        // Ordinary questions are not restricted by clinical case progression.
        if (caseQuestions.Count == 0)
        {
            return true;
        }

        var progressRecords = await _db.ExamSessionCaseProgress
            .AsNoTracking()
            .Where(p =>
                p.ExamSessionId == sessionId &&
                caseQuestions.Select(cq => cq.ClinicalCaseId)
                    .Contains(p.ClinicalCaseId))
            .Select(p => new
            {
                p.ClinicalCaseId,
                p.CurrentStageId
            })
            .ToListAsync(cancellationToken);

        foreach (var caseQuestion in caseQuestions)
        {
            var progress = progressRecords.FirstOrDefault(p =>
                p.ClinicalCaseId == caseQuestion.ClinicalCaseId);

            // Missing progress means the case has not been initialized
            // for this session, so its question must remain inaccessible.
            if (progress is null)
            {
                return false;
            }

            // Only questions in the currently released stage are accessible.
            if (progress.CurrentStageId != caseQuestion.ClinicalCaseStageId)
            {
                return false;
            }
        }

        return true;
    }

public async Task<ClinicalCaseAdvanceResult> AdvanceToNextStageAsync(
    Guid sessionId,
    Guid caseId,
    CancellationToken cancellationToken = default)
{
    var session = await _db.ExamSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId)
        .Select(s => new
        {
            s.Status,
            s.ExamId,
            s.Exam.ExamProgramId
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (session is null)
    {
        throw new InvalidOperationException(
            "The exam session could not be found.");
    }

    if (session.Status != ExamSessionStatus.InProgress)
    {
        throw new InvalidOperationException(
            "Clinical case stages can only advance while the exam session is in progress.");
    }

    var clinicalCase = await _db.ClinicalCases
        .AsNoTracking()
        .Where(c => c.Id == caseId)
        .Select(c => new
        {
            c.Id,
            c.ExamProgramId,
            c.IsPublished
        })
        .FirstOrDefaultAsync(cancellationToken);
        
    if (clinicalCase is null ||
        !clinicalCase.IsPublished ||
        clinicalCase.ExamProgramId != session.ExamProgramId)
    {
        throw new InvalidOperationException(
            "The clinical case is unavailable for this exam.");
    }

    var caseQuestionIds = await _db.ClinicalCaseQuestions
        .Where(cq => cq.ClinicalCaseId == caseId)
        .Select(cq => cq.QuestionId)
        .Distinct()
        .ToListAsync(cancellationToken);

    if (caseQuestionIds.Count == 0)
    {
        throw new InvalidOperationException(
            "The clinical case has no questions.");
    }

    var assignedQuestionCount = await _db.ExamQuestions
        .CountAsync(
            eq => eq.ExamId == session.ExamId &&
                  caseQuestionIds.Contains(eq.QuestionId),
            cancellationToken);

    if (assignedQuestionCount != caseQuestionIds.Count)
    {
        throw new InvalidOperationException(
            "The clinical case questions are not fully assigned to this exam.");
    }

    var progress = await _db.ExamSessionCaseProgress
        .FirstOrDefaultAsync(
            p => p.ExamSessionId == sessionId &&
                 p.ClinicalCaseId == caseId,
            cancellationToken);

    if (progress is null)
    {
        return ClinicalCaseAdvanceResult.ProgressNotFound;
    }

    if (progress.IsCompleted)
    {
        return ClinicalCaseAdvanceResult.AlreadyCompleted;
    }

    var currentStage = await _db.ClinicalCaseStages
        .FirstOrDefaultAsync(
            s => s.Id == progress.CurrentStageId &&
                 s.ClinicalCaseId == caseId,
            cancellationToken);

    if (currentStage is null)
    {
        throw new InvalidOperationException(
            "The current clinical case stage could not be found.");
    }

    var requiredQuestionIds = await _db.ClinicalCaseQuestions
        .Where(cq =>
            cq.ClinicalCaseId == caseId &&
            cq.ClinicalCaseStageId == currentStage.Id)
        .Select(cq => cq.QuestionId)
        .Distinct()
        .ToListAsync(cancellationToken);

    if (requiredQuestionIds.Count == 0)
    {
        throw new InvalidOperationException(
            "The current clinical case stage has no questions.");
    }

    var answeredCount = await _db.ExamSessionResponses
        .Where(r =>
            r.ExamSessionId == sessionId &&
            requiredQuestionIds.Contains(r.QuestionId))
        .Select(r => r.QuestionId)
        .Distinct()
        .CountAsync(cancellationToken);

    if (answeredCount < requiredQuestionIds.Count)
    {
        throw new InvalidOperationException(
            "All questions in the current stage must be answered before advancing.");
    }

    var nextStage = await _db.ClinicalCaseStages
        .Where(s =>
            s.ClinicalCaseId == caseId &&
            s.DisplayOrder > currentStage.DisplayOrder)
        .OrderBy(s => s.DisplayOrder)
        .FirstOrDefaultAsync(cancellationToken);

    if (nextStage is null)
    {
        progress.IsCompleted = true;
        progress.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ClinicalCaseAdvanceResult.CaseCompleted;
    }

    progress.CurrentStageId = nextStage.Id;
    progress.LastUpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync(cancellationToken);

    return ClinicalCaseAdvanceResult.Advanced;
}



    public async Task<ClinicalCaseStageResponse?> GetCurrentStageAsync(
        Guid sessionId,
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new
            {
                s.Status,
                s.ExamId,
                s.Exam.ExamProgramId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        if (session.Status != ExamSessionStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Clinical case information is only available while the exam session is in progress.");
        }

        var clinicalCase = await _db.ClinicalCases
            .AsNoTracking()
            .Where(c =>
                c.Id == caseId &&
                c.IsPublished &&
                c.ExamProgramId == session.ExamProgramId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Introduction,
                c.ExamProgramId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (clinicalCase is null)
        {
            return null;
        }

        // Verify that this clinical case is actually assigned to the exam.
// Sharing an exam program is not sufficient.
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

// Do not expose information about a case that is not in this exam.
if (assignedQuestionCount == 0)
{
    return null;
}

// A case must not be partially assigned to an exam.
if (assignedQuestionCount != caseQuestionIds.Count)
{
    throw new InvalidOperationException(
        "Clinical case information is unavailable because the case is " +
        "only partially included in this exam.");
}

        var progress = await _db.ExamSessionCaseProgress
            .AsNoTracking()
            .Where(p =>
                p.ExamSessionId == sessionId &&
                p.ClinicalCaseId == caseId)
            .Select(p => new
            {
                p.CurrentStageId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (progress is null)
        {
            return null;
        }

        var stages = await _db.ClinicalCaseStages
            .AsNoTracking()
            .Where(s => s.ClinicalCaseId == caseId)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.ClinicalInformation,
                s.DisplayOrder
            })
            .ToListAsync(cancellationToken);




            

        var currentStageIndex = stages.FindIndex(
            s => s.Id == progress.CurrentStageId);

        if (currentStageIndex < 0)
        {
            throw new InvalidOperationException(
                "The current clinical case stage could not be found.");
        }

        var currentStage = stages[currentStageIndex];

        return new ClinicalCaseStageResponse
        {
            CaseId = clinicalCase.Id,
            CaseTitle = clinicalCase.Title,
            CaseIntroduction = clinicalCase.Introduction,
            StageId = currentStage.Id,
            StageTitle = currentStage.Title,
            ClinicalInformation = currentStage.ClinicalInformation,
            StageNumber = currentStageIndex + 1,
            TotalStages = stages.Count
        };
    }

}
