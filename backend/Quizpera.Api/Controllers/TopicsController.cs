using Quizpera.Api.Contracts.Topics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;

namespace Quizpera.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly QuizperaDbContext _db;

    public TopicsController(QuizperaDbContext db)
    {
        _db = db;
    }

   [HttpGet]
public async Task<IActionResult> GetTopics(Guid? subjectId)
{
    var query = _db.Topics.AsQueryable();

    if (subjectId.HasValue)
    {
        query = query.Where(t => t.SubjectId == subjectId.Value);
    }

    var topics = await query
        .Select(t => new
        {
            t.Id,
            t.Name,
            t.SubjectId
        })
        .ToListAsync();

    return Ok(topics);
}

[HttpPost]
public async Task<IActionResult> CreateTopic(
    CreateTopicRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return BadRequest(new
        {
            error = "Topic name is required."
        });
    }

    var normalizedName = request.Name.Trim();

    var subjectExists = await _db.Subjects
        .AnyAsync(s => s.Id == request.SubjectId);

    if (!subjectExists)
    {
        return BadRequest(new
        {
            error = "The selected subject does not exist."
        });
    }

    var exists = await _db.Topics
        .AnyAsync(t =>
            t.SubjectId == request.SubjectId &&
            t.Name.ToLower() == normalizedName.ToLower());

    if (exists)
    {
        return Conflict(new
        {
            error = "A topic with this name already exists for the selected subject."
        });
    }

    var topic = new Quizpera.Api.Domain.Questions.Topic
    {
        Id = Guid.NewGuid(),
        Name = normalizedName,
        SubjectId = request.SubjectId
    };

    _db.Topics.Add(topic);

    await _db.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetTopics),
        new { id = topic.Id },
        new
        {
            topic.Id,
            topic.Name,
            topic.SubjectId
        });
}

}