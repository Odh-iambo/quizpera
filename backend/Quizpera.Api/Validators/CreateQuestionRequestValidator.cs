using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Contracts.Questions;

namespace Quizpera.Api.Validators;

public class CreateQuestionRequestValidator
{
    public Dictionary<string, string[]> Validate(CreateQuestionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Stem))
        {
            errors["stem"] = ["Question stem is required."];
        }

        if (request.Type == QuestionType.SingleChoice &&
    request.Options is not null)
{
    var correctOptionCount = request.Options.Count(option => option.IsCorrect);

    if (correctOptionCount != 1)
    {
        errors["options"] =
        ["A single-choice question must have exactly one correct option."];
    }
}

        if (request.Options is null || request.Options.Count < 2)
        {
            errors["options"] = ["A question must contain at least two options."];
        }

        if (request.Options is not null)
        {
            if (request.Options.Any(option =>
                string.IsNullOrWhiteSpace(option.Text)))
            {
                errors["options"] = ["Every option must contain text."];
            }

            var duplicateOrders = request.Options
                .GroupBy(option => option.DisplayOrder)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateOrders.Count > 0)
            {
                errors["options"] =
                [
                    "Option display orders must be unique."
                ];
            }
        }

        return errors;
    }
}