namespace Quizpera.Api.Domain.Questions;

public class ExamSessionQuestionFlag
{
    public Guid ExamSessionId { get; set; }

    public ExamSession ExamSession { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public DateTime FlaggedAt { get; set; }
}