namespace Quizpera.Api.Domain.Questions;

public class QuestionOption
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}