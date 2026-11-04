using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Contracts.Questions;

public class BowTieOptionExamResponse
{
    public Guid Id { get; set; }

    public BowTieOptionCategory Category { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}
