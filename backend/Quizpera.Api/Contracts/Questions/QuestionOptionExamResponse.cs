namespace Quizpera.Api.Contracts.Questions;

public class QuestionOptionExamResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}