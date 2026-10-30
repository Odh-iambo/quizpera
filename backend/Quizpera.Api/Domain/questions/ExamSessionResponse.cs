namespace Quizpera.Api.Domain.Questions;

public class ExamSessionResponse
{
    public Guid Id { get; set; }

    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    // Optional for Numeric Entry; retained for option-based questions.
    public Guid? SelectedOptionId { get; set; }
    public QuestionOption? SelectedOption { get; set; }

    // Stores the student's submitted numeric answer when applicable.
    public decimal? NumericAnswer { get; set; }

    // Supports multiple selected options for SATA and Ordered Response.
    public ICollection<ExamSessionResponseOption> SelectedOptions { get; set; }
        = new List<ExamSessionResponseOption>();


        // Stores the rationale options selected by the student.
public ICollection<ExamSessionResponseRationaleOption>
    SelectedRationaleOptions { get; set; }
    = new List<ExamSessionResponseRationaleOption>();

    // Stores the student's row-by-row Matrix/Grid answers.
public ICollection<ExamSessionMatrixAnswer> MatrixAnswers { get; set; }
    = new List<ExamSessionMatrixAnswer>();

    // Whether the submitted answer is completely correct.
    public bool IsCorrect { get; set; }

    // Score awarded by the question evaluator.
    public decimal PointsEarned { get; set; }

    // Maximum score available for this question.
    public decimal MaximumPoints { get; set; } = 1m;

    public DateTime AnsweredAt { get; set; }
}
