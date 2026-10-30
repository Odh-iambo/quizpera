namespace Quizpera.Api.Domain.Questions;

public class ClinicalCaseQuestion
{
    public Guid ClinicalCaseId { get; set; }

    public ClinicalCase ClinicalCase { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public Guid ClinicalCaseStageId { get; set; }

    public ClinicalCaseStage ClinicalCaseStage { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public ClinicalJudgmentFunction ClinicalJudgmentFunction { get; set; }
}
