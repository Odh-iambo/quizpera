namespace Quizpera.Api.Contracts.Exams;

public class CompleteExamSessionResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CompletedAt { get; set; }
}