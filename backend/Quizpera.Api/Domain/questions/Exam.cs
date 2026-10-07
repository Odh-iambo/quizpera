namespace Quizpera.Api.Domain.Questions;

public class Exam
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ExamType Type { get; set; }

    public Guid ExamProgramId { get; set; }

    public ExamProgram ExamProgram { get; set; } = null!;

    public ICollection<ExamQuestion> ExamQuestions { get; set; }
        = new List<ExamQuestion>();

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}