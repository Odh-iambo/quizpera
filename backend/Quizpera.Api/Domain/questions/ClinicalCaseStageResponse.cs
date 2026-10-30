namespace Quizpera.Api.Domain.Questions;

public sealed class ClinicalCaseStageResponse
{
    public Guid CaseId { get; set; }
    public string CaseTitle { get; set; } = string.Empty;
    public string CaseIntroduction { get; set; } = string.Empty;
    public Guid StageId { get; set; }
    public string StageTitle { get; set; } = string.Empty;
    public string ClinicalInformation { get; set; } = string.Empty;
    public int StageNumber { get; set; }
    public int TotalStages { get; set; }
}
