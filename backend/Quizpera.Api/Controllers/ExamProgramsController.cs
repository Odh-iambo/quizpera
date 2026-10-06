using Quizpera.Api.Contracts.ExamPrograms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;

namespace Quizpera.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamProgramsController : ControllerBase
{
    private readonly QuizperaDbContext _db;

    public ExamProgramsController(QuizperaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetExamPrograms()
    {
        var examPrograms = await _db.ExamPrograms
            .Select(p => new
            {
                p.Id,
                p.Name
            })
            .ToListAsync();

        return Ok(examPrograms);
    }
    
    [HttpPost]
public async Task<IActionResult> CreateExamProgram(
    CreateExamProgramRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return BadRequest(new
        {
            error = "Exam program name is required."
        });
    }

    var normalizedName = request.Name.Trim();

    var exists = await _db.ExamPrograms
        .AnyAsync(p => p.Name.ToLower() == normalizedName.ToLower());

    if (exists)
    {
        return Conflict(new
        {
            error = "An exam program with this name already exists."
        });
    }

    var examProgram = new Domain.Questions.ExamProgram
    {
        Id = Guid.NewGuid(),
        Name = normalizedName
    };

    _db.ExamPrograms.Add(examProgram);

    await _db.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetExamPrograms),
        new { id = examProgram.Id },
        new
        {
            examProgram.Id,
            examProgram.Name
        });
}
}