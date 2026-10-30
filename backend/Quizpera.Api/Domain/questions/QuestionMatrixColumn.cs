namespace Quizpera.Api.Domain.Questions;

public class QuestionMatrixColumn
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}
