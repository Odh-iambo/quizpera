namespace Quizpera.Api.Contracts.Questions;

public class QuestionOptionResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}