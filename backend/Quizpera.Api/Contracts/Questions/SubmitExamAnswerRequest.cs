namespace Quizpera.Api.Contracts.Questions;

public class SubmitExamAnswerRequest
{
    public Guid QuestionId { get; set; }

    // Single Choice and True/False
    public Guid? SelectedOptionId { get; set; }

    // Multiple Response (Select All That Apply)
    public List<Guid> SelectedOptionIds { get; set; } = [];

    // Ordered Response
    public List<Guid> OrderedOptionIds { get; set; } = [];

    // Numeric Entry / dosage calculations
    public decimal? NumericAnswer { get; set; }

    // Rationale scoring
    public List<Guid> SelectedRationaleOptionIds { get; set; } = [];

    // Matrix/Grid: one selected column per matrix row
    public List<MatrixAnswerRequest> MatrixAnswers { get; set; } = [];
}

public class MatrixAnswerRequest
{
    public Guid MatrixRowId { get; set; }

    public Guid SelectedColumnId { get; set; }
}