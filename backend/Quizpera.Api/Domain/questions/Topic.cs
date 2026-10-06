namespace Quizpera.Api.Domain.Questions;

public class Topic
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}