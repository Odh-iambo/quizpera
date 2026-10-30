namespace Quizpera.Api.Domain.Questions;

public class ExamSessionResponseRationaleOption
{
    public Guid ExamSessionResponseId { get; set; }
    public ExamSessionResponse ExamSessionResponse { get; set; } = null!;

    public Guid QuestionRationaleOptionId { get; set; }
    public QuestionRationaleOption QuestionRationaleOption { get; set; } = null!;

    public int? SelectedOrder { get; set; }
}
