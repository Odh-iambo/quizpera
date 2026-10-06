namespace Quizpera.Api.Domain.Questions;

public class Domain
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid ExamProgramId { get; set; }

    public ExamProgram ExamProgram { get; set; } = null!;

    public ICollection<Subject> Subjects { get; set; }
        = new List<Subject>();

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}