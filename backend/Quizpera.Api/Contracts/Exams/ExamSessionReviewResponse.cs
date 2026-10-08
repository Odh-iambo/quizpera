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

    public ExamSessionReviewOptionResponse? SelectedOption { get; set; }
    public ExamSessionReviewOptionResponse? CorrectOption { get; set; }

    public bool? IsCorrect { get; set; }
    public string? Rationale { get; set; }
}

public class ExamSessionReviewOptionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
}