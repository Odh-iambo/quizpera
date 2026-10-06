using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Validators;
using Quizpera.Api.Services;

namespace Quizpera.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
   private readonly QuizperaDbContext _db;
private readonly CreateQuestionRequestValidator _validator;
private readonly QuestionValidationService _questionValidationService;

public QuestionsController(
    QuizperaDbContext db,
    CreateQuestionRequestValidator validator,
    QuestionValidationService questionValidationService)
{
    _db = db;
    _validator = validator;
    _questionValidationService = questionValidationService;
}

   [HttpGet]
public async Task<IActionResult> GetQuestions()
{
    var questions = await _db.Questions
        .Include(q => q.Options)
        .Select(q => new QuestionResponse
        {
            Id = q.Id,
            Type = q.Type,
            Stem = q.Stem,
            Difficulty = q.Difficulty,
            Rationale = q.Rationale,
            Options = q.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new QuestionOptionResponse
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
public async Task<IActionResult> GetQuestion(Guid id)
{
    var question = await _db.Questions
        .Where(q => q.Id == id)
        .Select(q => new QuestionExamResponse
        {
            Id = q.Id,
            Type = q.Type,
            Stem = q.Stem,
            Difficulty = q.Difficulty,
            Options = q.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new QuestionOptionExamResponse
                {
                    Id = o.Id,
                    Text = o.Text,
                    DisplayOrder = o.DisplayOrder
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    if (question is null)
    {
        return NotFound();
    }

    return Ok(question);
}


[HttpGet("{id:guid}/review")]
public async Task<IActionResult> GetQuestionReview(Guid id)
{
    var question = await _db.Questions
        .Where(q => q.Id == id)
        .Select(q => new QuestionReviewResponse
        {
            Id = q.Id,
            Type = q.Type,
            Stem = q.Stem,
            Difficulty = q.Difficulty,
            Rationale = q.Rationale,
            Options = q.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new QuestionReviewOptionResponse
                {
                    Id = o.Id,
                    Text = o.Text,
                    DisplayOrder = o.DisplayOrder,
                    IsCorrect = o.IsCorrect
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    if (question is null)
    {
        return NotFound();
    }

    return Ok(question);
}

[HttpPost]
public async Task<IActionResult> CreateQuestion(
    CreateQuestionRequest request)
{
    var errors = _validator.Validate(request);

if (errors.Count > 0)
{
    return BadRequest(new
    {
        errors
    });
}

var relationshipErrors =
    await _questionValidationService.ValidateAsync(request);

if (relationshipErrors.Count > 0)
{
    return BadRequest(new
    {
        errors = relationshipErrors
    });
}

    var question = new Question
    {
        Id = Guid.NewGuid(),
        Type = request.Type,
        Stem = request.Stem,
        ExamProgramId = request.ExamProgramId,
        DomainId = request.DomainId,
        SubjectId = request.SubjectId,
        TopicId = request.TopicId,
        ClientNeedId = request.ClientNeedId,
        Difficulty = request.Difficulty,
        Rationale = request.Rationale
    };

    foreach (var option in request.Options)
    {
        question.Options.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            Text = option.Text,
            DisplayOrder = option.DisplayOrder,
            IsCorrect = option.IsCorrect
        });
    }

    _db.Questions.Add(question);

await _db.SaveChangesAsync();

var response = new QuestionResponse
{
    Id = question.Id,
    Type = question.Type,
    Stem = question.Stem,
    Difficulty = question.Difficulty,
    Rationale = question.Rationale,
    Options = question.Options
        .OrderBy(o => o.DisplayOrder)
        .Select(o => new QuestionOptionResponse
        {
            Id = o.Id,
            Text = o.Text,
            DisplayOrder = o.DisplayOrder
        })
        .ToList()
};

return CreatedAtAction(
    nameof(GetQuestion),
    new { id = question.Id },
    response);
}
}