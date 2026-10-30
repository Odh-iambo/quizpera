namespace Quizpera.Api.Domain.Questions;

public class ClinicalCase
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Introduction { get; set; } = null!;

    public Guid ExamProgramId { get; set; }

    public ExamProgram ExamProgram { get; set; } = null!;

    public Guid DomainId { get; set; }

    public Domain Domain { get; set; } = null!;

    public Guid? SubjectId { get; set; }

    public Subject? Subject { get; set; }

    public Difficulty Difficulty { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ClinicalCaseStage> Stages { get; set; }
        = new List<ClinicalCaseStage>();

    public ICollection<ClinicalCaseQuestion> Questions { get; set; }
        = new List<ClinicalCaseQuestion>();
}