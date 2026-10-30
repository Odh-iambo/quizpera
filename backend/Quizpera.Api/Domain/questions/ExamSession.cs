using Quizpera.Api.Domain.Auth;

namespace Quizpera.Api.Domain.Questions;

public class ExamSession
{
    public Guid Id { get; set; }

    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public ExamSessionStatus Status { get; set; }

    public ExamSessionMode Mode { get; set; } = ExamSessionMode.Exam;

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}