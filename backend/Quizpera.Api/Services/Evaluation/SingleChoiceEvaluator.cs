using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class SingleChoiceEvaluator : IQuestionEvaluator
{
    private readonly QuizperaDbContext _db;

    public SingleChoiceEvaluator(QuizperaDbContext db)
    {
        _db = db;
    }

    public QuestionType SupportedType => QuestionType.SingleChoice;

    public async Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request)
    {
        var option = await _db.QuestionOptions
            .FirstOrDefaultAsync(o =>
                o.Id == request.SelectedOptionId &&
                o.QuestionId == request.QuestionId);

        if (option is null)
        {
            return null;
        }

        return new QuestionEvaluationResult
        {
            QuestionId = request.QuestionId,
            SelectedOptionId = request.SelectedOptionId,
            IsCorrect = option.IsCorrect
        };
    }
}
