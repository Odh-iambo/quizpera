namespace Quizpera.Api.Contracts.Questions;

public class QuestionEvaluationResult
{
    public Guid QuestionId { get; set; }

    public Guid SelectedOptionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new();


    public List<Guid> SelectedRationaleOptionIds { get; set; } = new();

    public decimal? NumericAnswer { get; set; }

    public bool IsCorrect { get; set; }

    public decimal PointsEarned { get; set; }

    public decimal MaximumPoints { get; set; } = 1m;
}
