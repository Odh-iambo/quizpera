namespace Quizpera.Api.Contracts.Exams;
public class ExamSessionSavedMatrixAnswerResponse
{
    public Guid MatrixRowId { get; set; }

    public Guid SelectedColumnId { get; set; }

    public List<ExamSessionSavedMatrixAnswerResponse> MatrixAnswers { get; set; } = [];
}

public class ExamSessionQuestionStatusResponse
{
    public Guid QuestionId { get; set; }

    public int Position { get; set; }

    public bool IsAnswered { get; set; }
}