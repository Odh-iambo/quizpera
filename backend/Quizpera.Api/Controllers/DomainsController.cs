using Quizpera.Api.Contracts.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;

namespace Quizpera.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DomainsController : ControllerBase
{
    private readonly QuizperaDbContext _db;

    public DomainsController(QuizperaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetDomains()
    {
        var domains = await _db.Domains
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.ExamProgramId
            })
            .ToListAsync();

        return Ok(domains);
    }

    [HttpPost]
public async Task<IActionResult> CreateDomain(CreateDomainRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return BadRequest(new
        {
            error = "Domain name is required."
        });
    }

    var normalizedName = request.Name.Trim();

    var examProgramExists = await _db.ExamPrograms
        .AnyAsync(p => p.Id == request.ExamProgramId);

    if (!examProgramExists)
    {
        return BadRequest(new
        {
            error = "The selected exam program does not exist."
        });
    }

    var exists = await _db.Domains
        .AnyAsync(d =>
            d.ExamProgramId == request.ExamProgramId &&
            d.Name.ToLower() == normalizedName.ToLower());

    if (exists)
    {
        return Conflict(new
        {
            error = "A domain with this name already exists for the selected exam program."
        });
    }

    var domain = new Domain.Questions.Domain
    {
        Id = Guid.NewGuid(),
        Name = normalizedName,
        ExamProgramId = request.ExamProgramId
    };

    _db.Domains.Add(domain);

    await _db.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetDomains),
        new { id = domain.Id },
        new
        {
            domain.Id,
            domain.Name,
            domain.ExamProgramId
        });
}
}