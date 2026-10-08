namespace Quizpera.Api.Domain.Questions;

public class ExamSessionResponse
{
    public Guid Id { get; set; }

    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public Guid SelectedOptionId { get; set; }
    public QuestionOption SelectedOption { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAt { get; set; }
}