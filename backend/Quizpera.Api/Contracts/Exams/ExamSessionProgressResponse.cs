namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionProgressResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = null!;

    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int UnansweredQuestions { get; set; }

    public decimal ProgressPercentage { get; set; }
}