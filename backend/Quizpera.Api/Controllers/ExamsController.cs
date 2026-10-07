using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Exams;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Contracts.Questions;

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

    var examQuestion = new ExamQuestion
    {
        ExamId = examId,
        QuestionId = request.QuestionId
    };

    _db.ExamQuestions.Add(examQuestion);

    await _db.SaveChangesAsync();

    return Ok(new
    {
        examId,
        questionId = request.QuestionId
    });
}


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

[HttpPost("{examId:guid}/sessions")]
public async Task<IActionResult> StartExam(Guid examId)
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

    var session = new ExamSession
    {
        Id = Guid.NewGuid(),
        ExamId = examId,
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

[HttpGet("sessions/{sessionId:guid}")]
public async Task<IActionResult> GetExamSession(Guid sessionId)
{
    var session = await _db.ExamSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId)
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

}