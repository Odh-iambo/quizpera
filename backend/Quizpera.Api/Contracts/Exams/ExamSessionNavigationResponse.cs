namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionNavigationResponse
{
    public Guid SessionId { get; set; }

    public int CurrentPosition { get; set; }

    public int TotalQuestions { get; set; }

    public bool IsAnswered { get; set; }

    public ExamSessionQuestionResponse Question { get; set; } = null!;
}