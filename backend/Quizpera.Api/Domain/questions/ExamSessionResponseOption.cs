namespace Quizpera.Api.Domain.Questions;

public class ExamSessionResponseOption
{
    public Guid ExamSessionResponseId { get; set; }

    public ExamSessionResponse ExamSessionResponse { get; set; } = null!;

    public Guid QuestionOptionId { get; set; }

    public QuestionOption QuestionOption { get; set; } = null!;

    public int? SelectedOrder { get; set; }
}