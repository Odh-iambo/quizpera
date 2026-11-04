using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Validators;
using Quizpera.Api.Services;
using Quizpera.Api.Services.Evaluation;
using Microsoft.AspNetCore.Authorization;

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

    // GET: api/questions
    // Administrator question listing.
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetQuestions()
    {
        var questions = await _db.Questions
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
                    .ToList(),

                BowTieOptions = q.BowTieOptions
                    .OrderBy(o => o.Category)
                    .ThenBy(o => o.DisplayOrder)
                    .Select(o => new BowTieOptionExamResponse
                    {
                        Id = o.Id,
                        Category = o.Category,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(questions);
    }

    // GET: api/questions/{id}
    // Student-facing question delivery.
    [Authorize]
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
                MaximumPoints = q.MaximumPoints,
                ScoringMethod = q.ScoringMethod,

                // Correct numeric answers must never be exposed here.

                Options = q.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new QuestionOptionExamResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList(),

                BowTieOptions = q.BowTieOptions
                    .OrderBy(o => o.Category)
                    .ThenBy(o => o.DisplayOrder)
                    .Select(o => new BowTieOptionExamResponse
                    {
                        Id = o.Id,
                        Category = o.Category,
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

    // GET: api/questions/{id}/review
    // Administrator answer-key review.
    [Authorize(Roles = "Admin,SuperAdmin")]
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
                NumericCorrectAnswer = q.NumericCorrectAnswer,
                NumericTolerance = q.NumericTolerance,

                Options = q.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new QuestionReviewOptionResponse
                    {
                        Id = o.Id,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder,
                        IsCorrect = o.IsCorrect
                    })
                    .ToList(),

                BowTieOptions = q.BowTieOptions
                    .OrderBy(o => o.Category)
                    .ThenBy(o => o.DisplayOrder)
                    .Select(o => new BowTieOptionReviewResponse
                    {
                        Id = o.Id,
                        Category = o.Category,
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

    // POST: api/questions
    // Administrator question creation.
    [Authorize(Roles = "Admin,SuperAdmin")]
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
            MaximumPoints = request.MaximumPoints,
            ScoringMethod = request.ScoringMethod,
            Rationale = request.Rationale,
            NumericCorrectAnswer = request.NumericCorrectAnswer,
            NumericTolerance = request.NumericTolerance
        };

        // Create conventional options.
        if (request.Type != QuestionType.MatrixGrid &&
            request.Type != QuestionType.BowTie)
        {
            foreach (var option in request.Options ?? [])
            {
                question.Options.Add(new QuestionOption
                {
                    Id = Guid.NewGuid(),
                    Text = option.Text,
                    DisplayOrder = option.DisplayOrder,
                    CorrectOrder = option.CorrectOrder,
                    IsCorrect = option.IsCorrect
                });
            }
        }

        // Create Matrix/Grid rows and columns.
        if (request.Type == QuestionType.MatrixGrid)
        {
            var rows = request.MatrixRows!
                .Select(row => new QuestionMatrixRow
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Text = row.Text,
                    DisplayOrder = row.DisplayOrder
                })
                .ToList();

            var columns = request.MatrixColumns!
                .Select(column => new QuestionMatrixColumn
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Text = column.Text,
                    DisplayOrder = column.DisplayOrder
                })
                .ToList();

            foreach (var row in rows)
            {
                question.MatrixRows.Add(row);
            }

            foreach (var column in columns)
            {
                question.MatrixColumns.Add(column);
            }

            // Map each row to its correct column.
            foreach (var answer in request.MatrixAnswers!)
            {
                var matrixRow = rows[answer.RowIndex];

                var correctColumn = columns[answer.CorrectColumnIndex];

                _db.Set<QuestionMatrixAnswer>().Add(
                    new QuestionMatrixAnswer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        MatrixRowId = matrixRow.Id,
                        CorrectColumnId = correctColumn.Id
                    });
            }
        }

        // Create Bow-Tie options.
        if (request.Type == QuestionType.BowTie)
        {
            foreach (var option in request.BowTieOptions!)
            {
                question.BowTieOptions.Add(new BowTieOption
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Category = option.Category,
                    Text = option.Text,
                    DisplayOrder = option.DisplayOrder,
                    IsCorrect = option.IsCorrect
                });
            }
        }

        _db.Questions.Add(question);

        await _db.SaveChangesAsync();

        // Return student-safe option information.
        var response = new QuestionResponse
        {
            Id = question.Id,
            Type = question.Type,
            Stem = question.Stem,
            Difficulty = question.Difficulty,
            Rationale = question.Rationale,
            MaximumPoints = question.MaximumPoints,
            ScoringMethod = question.ScoringMethod,
            NumericCorrectAnswer = question.NumericCorrectAnswer,
            NumericTolerance = question.NumericTolerance,

            Options = question.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new QuestionOptionResponse
                {
                    Id = o.Id,
                    Text = o.Text,
                    DisplayOrder = o.DisplayOrder
                })
                .ToList(),

            BowTieOptions = question.BowTieOptions
                .OrderBy(o => o.Category)
                .ThenBy(o => o.DisplayOrder)
                .Select(o => new BowTieOptionExamResponse
                {
                    Id = o.Id,
                    Category = o.Category,
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

    // POST: api/questions/{id}/evaluate
    // Evaluate an answer.
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("{id:guid}/evaluate")]
    public async Task<IActionResult> EvaluateAnswer(
        Guid id,
        StudentAnswerRequest request,
        [FromServices] QuestionEvaluationService evaluationService)
    {
        if (id != request.QuestionId)
        {
            return BadRequest(new
            {
                error = "The question ID in the URL does not match the question ID in the request."
            });
        }

        var question = await _db.Questions
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question is null)
        {
            return NotFound(new
            {
                error = "The specified question was not found."
            });
        }

        var result = await evaluationService.EvaluateAsync(
            request,
            question.Type);

        if (result is null)
        {
            return BadRequest(new
            {
                error = "The selected option does not belong to the specified question."
            });
        }

        return Ok(result);
    }
}