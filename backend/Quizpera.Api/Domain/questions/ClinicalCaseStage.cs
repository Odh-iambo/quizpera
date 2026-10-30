namespace Quizpera.Api.Domain.Questions;

public class ClinicalCaseStage
{
    public Guid Id { get; set; }

    public Guid ClinicalCaseId { get; set; }

    public ClinicalCase ClinicalCase { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string ClinicalInformation { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public ICollection<ClinicalCaseQuestion> Questions { get; set; }
        = new List<ClinicalCaseQuestion>();
}
