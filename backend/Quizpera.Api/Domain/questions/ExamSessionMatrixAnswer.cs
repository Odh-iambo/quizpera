namespace Quizpera.Api.Domain.Questions;

public class ExamSessionMatrixAnswer
{
    public Guid ExamSessionResponseId { get; set; }

    public ExamSessionResponse ExamSessionResponse { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Guid MatrixRowId { get; set; }

    public QuestionMatrixRow MatrixRow { get; set; } = null!;

    public Guid SelectedColumnId { get; set; }

    public QuestionMatrixColumn SelectedColumn { get; set; } = null!;
}