namespace Quizpera.Api.Contracts.Domain;

public class CreateDomainRequest
{
    public string Name { get; set; } = null!;

    public Guid ExamProgramId { get; set; }
}