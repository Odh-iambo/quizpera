namespace Quizpera.Api.Domain.Questions;

public class QuestionMatrixAnswer
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public Guid MatrixRowId { get; set; }
    public QuestionMatrixRow MatrixRow { get; set; } = null!;

    public Guid CorrectColumnId { get; set; }
    public QuestionMatrixColumn CorrectColumn { get; set; } = null!;
}
