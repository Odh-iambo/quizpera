using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Contracts.Exams;

public class CreateExamRequest
{
    public string Name { get; set; } = null!;

    public ExamType Type { get; set; }

    public Guid ExamProgramId { get; set; }
}