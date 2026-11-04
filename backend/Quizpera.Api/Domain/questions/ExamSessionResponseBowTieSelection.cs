namespace Quizpera.Api.Domain.Questions;

public class ExamSessionResponseBowTieSelection
{
    public Guid ExamSessionResponseId { get; set; }

    public ExamSessionResponse ExamSessionResponse { get; set; } = null!;

    public Guid BowTieOptionId { get; set; }

    public BowTieOption BowTieOption { get; set; } = null!;
}
