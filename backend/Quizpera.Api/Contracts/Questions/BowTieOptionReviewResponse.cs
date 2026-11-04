using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Contracts.Questions;

public class BowTieOptionReviewResponse
{
    public Guid Id { get; set; }

    public BowTieOptionCategory Category { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}
