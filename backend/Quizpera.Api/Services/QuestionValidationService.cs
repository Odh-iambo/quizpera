using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;

namespace Quizpera.Api.Services;

public class QuestionValidationService
{
    private readonly QuizperaDbContext _db;

    public QuestionValidationService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, string[]>> ValidateAsync(
        CreateQuestionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var domain = await _db.Domains
    .FirstOrDefaultAsync(d => d.Id == request.DomainId);

if (domain is null)
{
    errors["domainId"] =
    ["The selected domain does not exist."];
}
else if (domain.ExamProgramId != request.ExamProgramId)
{
    errors["domainId"] =
    ["The selected domain does not belong to the selected exam program."];
}

        var domainExists = await _db.Domains
            .AnyAsync(d => d.Id == request.DomainId);

        if (!domainExists)
        {
            errors["domainId"] =
            ["The selected domain does not exist."];
        }

        var subject = await _db.Subjects
            .FirstOrDefaultAsync(s => s.Id == request.SubjectId);

        if (subject is null)
        {
            errors["subjectId"] =
            ["The selected subject does not exist."];
        }
        else if (subject.DomainId != request.DomainId)
        {
            errors["subjectId"] =
            ["The selected subject does not belong to the selected domain."];
        }

        if (request.TopicId.HasValue)
        {
            var topic = await _db.Topics
                .FirstOrDefaultAsync(t => t.Id == request.TopicId.Value);

            if (topic is null)
            {
                errors["topicId"] =
                ["The selected topic does not exist."];
            }
            else if (topic.SubjectId != request.SubjectId)
            {
                errors["topicId"] =
                ["The selected topic does not belong to the selected subject."];
            }
        }

        if (request.ClientNeedId.HasValue)
        {
            var clientNeedExists = await _db.ClientNeeds
                .AnyAsync(c => c.Id == request.ClientNeedId.Value);

            if (!clientNeedExists)
            {
                errors["clientNeedId"] =
                ["The selected client need does not exist."];
            }
        }

        return errors;
    }
}