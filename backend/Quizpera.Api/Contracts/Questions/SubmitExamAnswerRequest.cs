namespace Quizpera.Api.Contracts.Questions;

public class SubmitExamAnswerRequest
{
    public Guid QuestionId { get; set; }

    public Guid SelectedOptionId { get; set; }
}