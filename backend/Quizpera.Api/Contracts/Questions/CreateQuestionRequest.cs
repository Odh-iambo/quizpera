using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Contracts.Questions;

public class CreateQuestionRequest
{
    public QuestionType Type { get; set; }

    public string Stem { get; set; } = null!;

    public Guid ExamProgramId { get; set; }

    public Guid DomainId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? ClientNeedId { get; set; }

    public Difficulty Difficulty { get; set; }

    public string? Rationale { get; set; }

    public List<CreateQuestionOptionRequest> Options { get; set; } = [];
}

public class CreateQuestionOptionRequest
{
    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsCorrect { get; set; }
}