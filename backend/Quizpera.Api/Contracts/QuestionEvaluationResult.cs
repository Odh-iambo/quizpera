namespace Quizpera.Api.Contracts.Questions;

public class QuestionEvaluationResult
{
    public Guid QuestionId { get; set; }

    public Guid SelectedOptionId { get; set; }

    public bool IsCorrect { get; set; }
}