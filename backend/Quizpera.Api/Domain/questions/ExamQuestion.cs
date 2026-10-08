namespace Quizpera.Api.Domain.Questions;

public class ExamQuestion
{
    public Guid ExamId { get; set; }

    public Exam Exam { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public int DisplayOrder { get; set; }

    
}


