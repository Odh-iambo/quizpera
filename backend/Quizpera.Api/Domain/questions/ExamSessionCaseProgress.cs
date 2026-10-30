namespace Quizpera.Api.Domain.Questions;

public class ExamSessionCaseProgress
{
    public Guid ExamSessionId { get; set; }

    public ExamSession ExamSession { get; set; } = null!;

    public Guid ClinicalCaseId { get; set; }

    public ClinicalCase ClinicalCase { get; set; } = null!;

    public Guid CurrentStageId { get; set; }

    public ClinicalCaseStage CurrentStage { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
