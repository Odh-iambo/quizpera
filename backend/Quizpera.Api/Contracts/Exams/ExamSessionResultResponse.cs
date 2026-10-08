namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionResultResponse
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = null!;

    public int TotalQuestions { get; set; }

    public int AnsweredQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public int IncorrectAnswers { get; set; }

    public int UnansweredQuestions { get; set; }

    public decimal? ScorePercentage { get; set; }
}