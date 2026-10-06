namespace Quizpera.Api.Domain.Questions;

public class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid DomainId { get; set; }

    public Domain Domain { get; set; } = null!;

    public ICollection<Topic> Topics { get; set; }
        = new List<Topic>();

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}