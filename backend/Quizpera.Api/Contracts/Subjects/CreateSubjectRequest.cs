namespace Quizpera.Api.Contracts.Subjects;

public class CreateSubjectRequest
{
    public string Name { get; set; } = null!;

    public Guid DomainId { get; set; }
}