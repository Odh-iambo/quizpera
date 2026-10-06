namespace Quizpera.Api.Contracts.Topics;

public class CreateTopicRequest
{
    public string Name { get; set; } = null!;

    public Guid SubjectId { get; set; }
}
