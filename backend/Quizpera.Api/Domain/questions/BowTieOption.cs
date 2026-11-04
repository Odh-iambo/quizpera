namespace Quizpera.Api.Domain.Questions;

public class BowTieOption
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public BowTieOptionCategory Category { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}