namespace Quizpera.Api.Contracts.Questions;

public class StudentAnswerRequest
{
    public Guid QuestionId { get; set; }

    public Guid SelectedOptionId { get; set; }
}