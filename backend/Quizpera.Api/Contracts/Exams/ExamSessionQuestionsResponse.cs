namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionQuestionsResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = null!;
    public List<ExamSessionQuestionResponse> Questions { get; set; } = [];
}

public class ExamSessionQuestionResponse
{
    public Guid QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string Stem { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public List<ExamSessionQuestionOptionResponse> Options { get; set; } = [];
}

public class ExamSessionQuestionOptionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
    public int DisplayOrder { get; set; }
}