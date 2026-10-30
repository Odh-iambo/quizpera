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

    public decimal MaximumPoints { get; set; } = 1m;


    public QuestionScoringMethod ScoringMethod { get; set; }
    = QuestionScoringMethod.AllOrNothing;

    public string? Rationale { get; set; }

    public decimal? NumericCorrectAnswer { get; set; }

    public decimal? NumericTolerance { get; set; }

    public ICollection<QuestionOption> Options { get; set; }
        = new List<QuestionOption>();

        public ICollection<QuestionRationaleOption> RationaleOptions { get; set; }
    = new List<QuestionRationaleOption>();

    public ICollection<ExamQuestion> ExamQuestions { get; set; }
        = new List<ExamQuestion>();
        public ICollection<QuestionMatrixRow> MatrixRows { get; set; }
    = new List<QuestionMatrixRow>();

public ICollection<QuestionMatrixColumn> MatrixColumns { get; set; }
    = new List<QuestionMatrixColumn>();
}