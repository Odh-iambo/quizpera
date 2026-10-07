namespace Quizpera.Api.Contracts.Exams;

public class StartExamResponse
{
    public Guid SessionId { get; set; }
    public Guid ExamId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime StartedAt { get; set; }
}