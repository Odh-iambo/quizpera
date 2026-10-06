using Quizpera.Api.Contracts.Subjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;

namespace Quizpera.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly QuizperaDbContext _db;

    public SubjectsController(QuizperaDbContext db)
    {
        _db = db;
    }

   [HttpGet]
public async Task<IActionResult> GetSubjects(Guid? domainId)
{
    var query = _db.Subjects.AsQueryable();

    if (domainId.HasValue)
    {
        query = query.Where(s => s.DomainId == domainId.Value);
    }

    var subjects = await query
        .Select(s => new
        {
            s.Id,
            s.Name,
            s.DomainId
        })
        .ToListAsync();

    return Ok(subjects);
}

[HttpPost]
public async Task<IActionResult> CreateSubject(CreateSubjectRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return BadRequest(new
        {
            error = "Subject name is required."
        });
    }

    var normalizedName = request.Name.Trim();

    var domainExists = await _db.Domains
        .AnyAsync(d => d.Id == request.DomainId);

    if (!domainExists)
    {
        return BadRequest(new
        {
            error = "The selected domain does not exist."
        });
    }

    var exists = await _db.Subjects
        .AnyAsync(s =>
            s.DomainId == request.DomainId &&
            s.Name.ToLower() == normalizedName.ToLower());

    if (exists)
    {
        return Conflict(new
        {
            error = "A subject with this name already exists for the selected domain."
        });
    }

    var subject = new Quizpera.Api.Domain.Questions.Subject
    {
        Id = Guid.NewGuid(),
        Name = normalizedName,
        DomainId = request.DomainId
    };

    _db.Subjects.Add(subject);

    await _db.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetSubjects),
        new { id = subject.Id },
        new
        {
            subject.Id,
            subject.Name,
            subject.DomainId
        });
}

}