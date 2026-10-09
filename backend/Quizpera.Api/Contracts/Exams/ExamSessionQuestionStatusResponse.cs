namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionQuestionStatusResponse
{
    public Guid QuestionId { get; set; }

    public int Position { get; set; }

    public bool IsAnswered { get; set; }
}