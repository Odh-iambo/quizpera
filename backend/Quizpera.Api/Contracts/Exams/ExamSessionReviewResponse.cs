namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionReviewResponse
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = null!;

    public List<ExamSessionReviewQuestionResponse> Questions { get; set; } = [];
}

public class ExamSessionReviewQuestionResponse
{
    public Guid QuestionId { get; set; }

    public string Stem { get; set; } = null!;

    public string Difficulty { get; set; } = null!;

    public string Type { get; set; } = null!;

    // Existing properties retained for Single Choice compatibility.
    public ExamSessionReviewOptionResponse? SelectedOption { get; set; }

    public ExamSessionReviewOptionResponse? CorrectOption { get; set; }

    // Support for multiple-response and ordered-response questions.
    public List<ExamSessionReviewOptionResponse> SelectedOptions { get; set; } = [];

    public List<ExamSessionReviewOptionResponse> CorrectOptions { get; set; } = [];

    // Rationale-scoring review.
    public List<ExamSessionReviewRationaleOptionResponse> SelectedRationaleOptions
        { get; set; } = [];

    public List<ExamSessionReviewRationaleOptionResponse> CorrectRationaleOptions
        { get; set; } = [];

    // Scoring and answer feedback.
    public bool? IsCorrect { get; set; }

    public decimal? PointsEarned { get; set; }

    public decimal? MaximumPoints { get; set; }

    // Numeric Entry review fields.
    public decimal? NumericAnswer { get; set; }

    public decimal? CorrectNumericAnswer { get; set; }

    // Explanatory rationale for the question.
    public string? Rationale { get; set; }
}

public class ExamSessionReviewOptionResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int? CorrectOrder { get; set; }
}

public class ExamSessionReviewRationaleOptionResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}