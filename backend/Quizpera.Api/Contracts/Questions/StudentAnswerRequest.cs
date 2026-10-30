namespace Quizpera.Api.Contracts.Questions;

public class StudentAnswerRequest
{
    public Guid QuestionId { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new();

    public List<Guid> SelectedRationaleOptionIds { get; set; } = new();

    public List<Guid> OrderedOptionIds { get; set; } = new();

    public decimal? NumericAnswer { get; set; }
}
