using Quizpera.Api.Domain.Questions;
namespace Quizpera.Api.Contracts.Questions;

public class QuestionResponse
{
    public Guid Id { get; set; }

    public QuestionType Type { get; set; }

    public string Stem { get; set; } = null!;

    public Difficulty Difficulty { get; set; }

    public string? Rationale { get; set; }

    public List<QuestionOptionResponse> Options { get; set; } = [];
}