namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionSubmissionReviewResponse
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = null!;

    public int TotalQuestions { get; set; }

    public int AnsweredQuestions { get; set; }

    public int UnansweredQuestions { get; set; }

    public int FlaggedQuestions { get; set; }

    public bool HasUnansweredQuestions { get; set; }

    public List<ExamSessionSubmissionReviewQuestionResponse> Questions
    {
        get;
        set;
    } = [];
}

public class ExamSessionSubmissionReviewQuestionResponse
{
    public Guid QuestionId { get; set; }

    public int Position { get; set; }

    public bool IsAnswered { get; set; }

    public bool IsFlagged { get; set; }
}