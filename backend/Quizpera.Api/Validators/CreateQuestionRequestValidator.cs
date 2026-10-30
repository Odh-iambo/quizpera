using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Validators;

public class CreateQuestionRequestValidator
{
    public Dictionary<string, string[]> Validate(CreateQuestionRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = [];
                errors[key] = messages;
            }

            if (!messages.Contains(message))
            {
                messages.Add(message);
            }
        }

        if (string.IsNullOrWhiteSpace(request.Stem))
        {
            AddError("stem", "Question stem is required.");
        }

        if (!Enum.IsDefined(typeof(QuestionType), request.Type))
        {
            AddError("type", "The specified question type is not supported.");

            return errors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToArray());
        }

        if (request.MaximumPoints <= 0)
        {
            AddError(
                "maximumPoints",
                "Maximum points must be greater than zero.");
        }

        var supportedScoringMethods = request.Type switch
        {
            QuestionType.SingleChoice =>
            [
                QuestionScoringMethod.AllOrNothing,
                QuestionScoringMethod.RationaleScoring
            ],

            QuestionType.MultipleResponse =>
            [
                QuestionScoringMethod.AllOrNothing,
                QuestionScoringMethod.ZeroOne,
                QuestionScoringMethod.PlusMinus
            ],

            QuestionType.TrueFalse =>
            [
                QuestionScoringMethod.AllOrNothing
            ],

            QuestionType.OrderedResponse =>
            [
                QuestionScoringMethod.AllOrNothing
            ],

            QuestionType.NumericEntry =>
            [
                QuestionScoringMethod.AllOrNothing
            ],

            _ => Array.Empty<QuestionScoringMethod>()
        };

        if (!Enum.IsDefined(
                typeof(QuestionScoringMethod),
                request.ScoringMethod) ||
            !supportedScoringMethods.Contains(request.ScoringMethod))
        {
            AddError(
                "scoringMethod",
                $"Scoring method '{request.ScoringMethod}' is not supported " +
                $"for question type '{request.Type}'.");
        }

        var options = request.Options;

        if (request.Type == QuestionType.NumericEntry)
        {
            if (!request.NumericCorrectAnswer.HasValue)
            {
                AddError(
                    "numericCorrectAnswer",
                    "A correct numeric answer is required.");
            }

            if (request.NumericTolerance.HasValue &&
                request.NumericTolerance.Value < 0)
            {
                AddError(
                    "numericTolerance",
                    "Numeric tolerance cannot be negative.");
            }

            if (options is { Count: > 0 })
            {
                AddError(
                    "options",
                    "Numeric Entry questions must not contain selectable options.");
            }
        }
        else
        {
            if (options is null || options.Count < 2)
            {
                AddError(
                    "options",
                    "A question must contain at least two options.");
            }

            if (options is not null)
            {
                if (options.Any(option =>
                    string.IsNullOrWhiteSpace(option.Text)))
                {
                    AddError(
                        "options",
                        "Every option must contain text.");
                }

                if (options
                    .GroupBy(option => option.DisplayOrder)
                    .Any(group => group.Count() > 1))
                {
                    AddError(
                        "options",
                        "Option display orders must be unique.");
                }
            }
        }

        if (options is not null)
        {
            var correctOptionCount =
                options.Count(option => option.IsCorrect);

            switch (request.Type)
            {
                case QuestionType.SingleChoice:
                    if (correctOptionCount != 1)
                    {
                        AddError(
                            "options",
                            "A single-choice question must have exactly one correct option.");
                    }

                    break;

                case QuestionType.MultipleResponse:
                    if (correctOptionCount < 1)
                    {
                        AddError(
                            "options",
                            "A multiple-response question must have at least one correct option.");
                    }

                    break;

                case QuestionType.TrueFalse:
                    var hasOneTrue = options.Count(option =>
                        string.Equals(
                            option.Text?.Trim(),
                            "True",
                            StringComparison.OrdinalIgnoreCase)) == 1;

                    var hasOneFalse = options.Count(option =>
                        string.Equals(
                            option.Text?.Trim(),
                            "False",
                            StringComparison.OrdinalIgnoreCase)) == 1;

                    if (options.Count != 2 || !hasOneTrue || !hasOneFalse)
                    {
                        AddError(
                            "options",
                            "A true/false question must have exactly two options: True and False.");
                    }

                    if (correctOptionCount != 1)
                    {
                        AddError(
                            "options",
                            "A true/false question must have exactly one correct option.");
                    }

                    break;
            }
        }

        if (request.Type == QuestionType.SingleChoice &&
            request.ScoringMethod == QuestionScoringMethod.RationaleScoring &&
            (request.RationaleOptions is null ||
             request.RationaleOptions.Count == 0))
        {
            AddError(
                "rationaleOptions",
                "Rationale scoring requires at least one rationale option.");
        }

        return errors.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToArray());
    }
}