namespace Quizpera.Api.Domain.Questions;

public class ClientNeed
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}