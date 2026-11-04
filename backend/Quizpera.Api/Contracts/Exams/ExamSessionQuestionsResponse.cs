namespace Quizpera.Api.Contracts.Exams;

public class ExamSessionQuestionsResponse
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = null!;

    public List<ExamSessionQuestionResponse> Questions { get; set; } = [];
}

public class ExamSessionQuestionMatrixRowResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}

public class ExamSessionQuestionMatrixColumnResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}

public class ExamSessionQuestionResponse
{
    public Guid QuestionId { get; set; }

    public int DisplayOrder { get; set; }

    public string Type { get; set; } = null!;

    public string Stem { get; set; } = null!;

    public string Difficulty { get; set; } = null!;

    public List<ExamSessionQuestionOptionResponse> Options { get; set; } = [];

    public List<ExamSessionQuestionMatrixRowResponse> MatrixRows { get; set; } = [];

    public List<ExamSessionQuestionMatrixColumnResponse> MatrixColumns { get; set; } = [];

    // NGN Bow-Tie options. Correctness is deliberately excluded.
    public List<ExamSessionQuestionBowTieOptionResponse> BowTieOptions { get; set; } = [];

    public ExamSessionSavedAnswerResponse? SavedAnswer { get; set; }
}

public class ExamSessionQuestionOptionResponse
{
    public Guid Id { get; set; }

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}

public class ExamSessionQuestionBowTieOptionResponse
{
    public Guid Id { get; set; }

    public string Category { get; set; } = null!;

    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }
}

public class ExamSessionSavedAnswerResponse
{
    public Guid? SelectedOptionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = [];

    public List<int?> SelectedOrders { get; set; } = [];

    public List<ExamSessionSavedMatrixAnswerResponse> MatrixAnswers { get; set; } = [];

    // Previously selected NGN Bow-Tie options.
    public List<Guid> BowTieOptionIds { get; set; } = [];

    public decimal? NumericAnswer { get; set; }

    public DateTime AnsweredAt { get; set; }
}