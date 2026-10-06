namespace Quizpera.Api.Domain.Questions;

public class Question
{
    public Guid Id { get; set; }

    public QuestionType Type { get; set; }

    public string Stem { get; set; } = null!;

    public Guid ExamProgramId { get; set; }

    public ExamProgram ExamProgram { get; set; } = null!;

    public Guid DomainId { get; set; }

    public Domain Domain { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public Guid? TopicId { get; set; }

    public Topic? Topic { get; set; }

    public Guid? ClientNeedId { get; set; }

    public ClientNeed? ClientNeed { get; set; }

    public Difficulty Difficulty { get; set; }

    public string? Rationale { get; set; }

    public ICollection<QuestionOption> Options { get; set; }
        = new List<QuestionOption>();

    public ICollection<ExamQuestion> ExamQuestions { get; set; }
        = new List<ExamQuestion>();
}