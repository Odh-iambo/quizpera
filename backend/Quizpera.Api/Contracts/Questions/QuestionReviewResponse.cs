using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Contracts.Questions;

public class QuestionReviewResponse
{
    public Guid Id { get; set; }

    public QuestionType Type { get; set; }

    public string Stem { get; set; } = null!;

    public Difficulty Difficulty { get; set; }

    public string? Rationale { get; set; }

    public List<QuestionReviewOptionResponse> Options { get; set; } = [];
}

public class QuestionReviewOptionResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}