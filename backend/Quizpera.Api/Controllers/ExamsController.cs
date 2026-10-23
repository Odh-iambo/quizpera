using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Quizpera.Api.Contracts.Exams;

using Quizpera.Api.Data;

using Quizpera.Api.Domain.Questions;

using Quizpera.Api.Contracts.Questions;

using Quizpera.Api.Services;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace Quizpera.Api.Controllers;

[ApiController]

[Route("api/exams")]

public class ExamsController : ControllerBase

{

    private readonly QuizperaDbContext _db;

    public ExamsController(QuizperaDbContext db)

    {

        _db = db;

    }
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]

    public async Task<IActionResult> CreateExam(

            CreateExamRequest request)

    {

        var examProgramExists = await _db.ExamPrograms

                    .AnyAsync(p => p.Id == request.ExamProgramId);

        if (!examProgramExists)

        {

            return BadRequest(new

            {

                error = "The selected exam program does not exist."

            });

        }

        var exam = new Exam

        {

            Id = Guid.NewGuid(),

            Name = request.Name,

            Type = request.Type,

            ExamProgramId = request.ExamProgramId

        };

        _db.Exams.Add(exam);

        await _db.SaveChangesAsync();

        return CreatedAtAction(

        nameof(GetExam),

        new { id = exam.Id },

                    exam);

    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("{examId:guid}/questions")]

    public async Task<IActionResult> AddQuestionToExam(

        Guid examId,

        AddExamQuestionRequest request)

    {

        var examExists = await _db.Exams

                .AnyAsync(e => e.Id == examId);

        if (!examExists)

        {

            return NotFound(new

            {

                error = "The specified exam was not found."

            });

        }

        var questionExists = await _db.Questions

                .AnyAsync(q => q.Id == request.QuestionId);

        if (!questionExists)

        {

            return NotFound(new

            {

                error = "The specified question was not found."

            });

        }

        var alreadyAssigned = await _db.ExamQuestions

                .AnyAsync(eq =>

                    eq.ExamId == examId &&

                    eq.QuestionId == request.QuestionId);

        if (alreadyAssigned)

        {

            return Conflict(new

            {

                error = "The question is already assigned to this exam."

            });

        }

        var nextDisplayOrder = await _db.ExamQuestions

                .Where(eq => eq.ExamId == examId)

                .Select(eq => (int?)eq.DisplayOrder)

                .MaxAsync() ?? 0;

        nextDisplayOrder++;

        var examQuestion = new ExamQuestion

        {

            ExamId = examId,

            QuestionId = request.QuestionId,

            DisplayOrder = nextDisplayOrder

        };

        _db.ExamQuestions.Add(examQuestion);

        await _db.SaveChangesAsync();

        return Ok(new

        {

            examQuestion.ExamId,

            examQuestion.QuestionId,

            examQuestion.DisplayOrder

        });

    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("{examId:guid}/questions")]

    public async Task<IActionResult> GetExamQuestions(Guid examId)

    {

        var examExists = await _db.Exams

                .AnyAsync(e => e.Id == examId);

        if (!examExists)

        {

            return NotFound(new

            {

                error = "The specified exam was not found."

            });

        }

        var questions = await _db.ExamQuestions

                .Where(eq => eq.ExamId == examId)

                .OrderBy(eq => eq.QuestionId)

                .Select(eq => new QuestionExamResponse

                {

                    Id = eq.Question.Id,

                    Type = eq.Question.Type,

                    Stem = eq.Question.Stem,

                    Difficulty = eq.Question.Difficulty,

                    Options = eq.Question.Options

                        .OrderBy(o => o.DisplayOrder)

                        .Select(o => new QuestionOptionExamResponse

                        {

                            Id = o.Id,

                            Text = o.Text,

                            DisplayOrder = o.DisplayOrder

                        })

                        .ToList()

                })

                .ToListAsync();

        return Ok(questions);

    }

    [HttpGet("{id:guid}")]

    public async Task<IActionResult> GetExam(Guid id)

    {

        var exam = await _db.Exams

                    .AsNoTracking()

                    .Where(e => e.Id == id)

                    .Select(e => new

                    {

                        e.Id,

                        e.Name,

                        e.Type,

                        e.ExamProgramId

                    })

                    .FirstOrDefaultAsync();

        if (exam is null)

        {

            return NotFound(new

            {

                error = "The specified exam was not found."

            });

        }

        return Ok(exam);

    }

    [Authorize(Roles = "Student,Admin,SuperAdmin")]

    [HttpPost("{examId:guid}/sessions")]

    public async Task<IActionResult> StartExam(Guid examId)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        var examExists = await _db.Exams

                .AnyAsync(e => e.Id == examId);

        if (!examExists)

        {

            return NotFound(new

            {

                error = "The specified exam was not found."

            });

        }

        var session = new ExamSession

        {

            Id = Guid.NewGuid(),

            ExamId = examId,

            UserId = userId,

            Status = ExamSessionStatus.InProgress,

            StartedAt = DateTime.UtcNow

        };

        _db.ExamSessions.Add(session);

        await _db.SaveChangesAsync();

        var response = new StartExamResponse

        {

            SessionId = session.Id,

            ExamId = session.ExamId,

            Status = session.Status.ToString(),

            StartedAt = session.StartedAt

        };

        return CreatedAtAction(

        nameof(GetExamSession),

        new { sessionId = session.Id },

                response);

    }

    [Authorize]

    [HttpGet("sessions/{sessionId:guid}")]

    public async Task<IActionResult> GetExamSession(Guid sessionId)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        var session = await _db.ExamSessions

                .AsNoTracking()

                .Where(s => s.Id == sessionId && s.UserId == userId)

                .Select(s => new StartExamResponse

                {

                    SessionId = s.Id,

                    ExamId = s.ExamId,

                    Status = s.Status.ToString(),

                    StartedAt = s.StartedAt

                })

                .FirstOrDefaultAsync();

        if (session is null)

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        return Ok(session);

    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/responses")]

    public async Task<IActionResult> SubmitAnswer(

        Guid sessionId,

        SubmitExamAnswerRequest request,

        [FromServices] ExamSessionResponseService responseService)

    {

        try

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))

            {

                return Unauthorized();

            }

            var response = await responseService.SubmitAnswerAsync(

                sessionId,

                userId,

                request);

            if (response is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(new
            {
                response.Id,
                response.ExamSessionId,
                response.QuestionId,
                response.SelectedOptionId,
                response.AnsweredAt
            });

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/complete")]

    public async Task<IActionResult> CompleteSession(

        Guid sessionId,

        [FromServices] ExamSessionResponseService responseService)

    {

        try

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))

            {

                return Unauthorized();

            }

            var session = await responseService.CompleteSessionAsync(

                sessionId,

                userId);

            if (session is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(new CompleteExamSessionResponse

            {

                SessionId = session.Id,

                Status = session.Status.ToString(),

                CompletedAt = session.CompletedAt!.Value

            });

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]

    [HttpGet("sessions/{sessionId:guid}/result")]

    public async Task<IActionResult> GetSessionResult(

        Guid sessionId,

        [FromServices] ExamSessionResultService resultService,

        [FromServices] ExamSessionOwnershipService ownershipService)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        var result = await resultService.GetResultAsync(sessionId);

        if (result is null)

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        return Ok(result);

    }

    [Authorize]

    [HttpGet("sessions/{sessionId:guid}/review")]

    public async Task<IActionResult> GetSessionReview(

        Guid sessionId,

        [FromServices] ExamSessionReviewService reviewService,

        [FromServices] ExamSessionOwnershipService ownershipService)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        try

        {

            var review = await reviewService.GetReviewAsync(sessionId);

            if (review is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(review);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]

    [HttpGet("sessions/{sessionId:guid}/progress")]

    public async Task<IActionResult> GetSessionProgress(

        Guid sessionId,

        [FromServices] ExamSessionProgressService progressService)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        var isOwner = await _db.ExamSessions

        .AsNoTracking()

        .AnyAsync(s => s.Id == sessionId && s.UserId == userId);

        if (!isOwner)

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        var progress = await progressService.GetProgressAsync(sessionId);

        if (progress is null)

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        return Ok(progress);

    }

    [Authorize]

    [HttpGet("sessions/{sessionId:guid}/questions")]

    public async Task<IActionResult> GetSessionQuestions(

        Guid sessionId,

        [FromServices] ExamSessionQuestionService questionService,

        [FromServices] ExamSessionOwnershipService ownershipService)

    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))

        {

            return Unauthorized();

        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))

        {

            return NotFound(new

            {

                error = "The specified exam session was not found."

            });

        }

        try

        {

            var questions = await questionService.GetQuestionsAsync(sessionId);

            if (questions is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(questions);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}/questions/{position:int}")]

    public async Task<IActionResult> GetSessionQuestion(

        Guid sessionId,

    int position,

        [FromServices] ExamSessionNavigationService navigationService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            var result = await navigationService.GetQuestionAsync(

                        sessionId,

                        position);

            if (result is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(result);

        }

        catch (ArgumentOutOfRangeException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}/question-statuses")]

    public async Task<IActionResult> GetSessionQuestionStatuses(

        Guid sessionId,

        [FromServices] ExamSessionQuestionStatusService statusService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            var statuses = await statusService.GetQuestionStatusesAsync(sessionId);

            if (statuses is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(statuses);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

    [Authorize]
    [HttpPut("sessions/{sessionId:guid}/questions/{questionId:guid}/flag")]

    public async Task<IActionResult> FlagQuestion(

        Guid sessionId,

        Guid questionId,

        [FromServices] ExamSessionQuestionFlagService flagService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            var created = await flagService.FlagQuestionAsync(

                        sessionId,

                        questionId);

            return Ok(new

            {

                sessionId,

                questionId,

                isFlagged = true,

                created

            });

        }

        catch (KeyNotFoundException ex)

        {

            return NotFound(new { error = ex.Message });

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new { error = ex.Message });

        }

    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}/questions/{questionId:guid}/flag")]

    public async Task<IActionResult> UnflagQuestion(

        Guid sessionId,

        Guid questionId,

        [FromServices] ExamSessionQuestionFlagService flagService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            await flagService.UnflagQuestionAsync(

                        sessionId,

                        questionId);

            return Ok(new

            {

                sessionId,

                questionId,

                isFlagged = false

            });

        }

        catch (KeyNotFoundException ex)

        {

            return NotFound(new { error = ex.Message });

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new { error = ex.Message });

        }

    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}/flagged-questions")]

    public async Task<IActionResult> GetFlaggedQuestions(

        Guid sessionId,

        [FromServices] ExamSessionQuestionFlagService flagService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            var flaggedQuestions = await flagService.GetFlaggedQuestionsAsync(

                        sessionId);

            if (flaggedQuestions is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(flaggedQuestions);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new { error = ex.Message });

        }

    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}/submission-review")]

    public async Task<IActionResult> GetSubmissionReview(

        Guid sessionId,

        [FromServices] ExamSessionSubmissionReviewService reviewService,
        [FromServices] ExamSessionOwnershipService ownershipService)

    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!await ownershipService.IsOwnerAsync(sessionId, userId))
        {
            return NotFound(new
            {
                error = "The specified exam session was not found."
            });
        }

        try

        {

            var review = await reviewService.GetReviewAsync(sessionId);

            if (review is null)

            {

                return NotFound(new

                {

                    error = "The specified exam session was not found."

                });

            }

            return Ok(review);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(new

            {

                error = ex.Message

            });

        }

    }

}