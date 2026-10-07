using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public class QuestionEvaluationService
{
    private readonly IEnumerable<IQuestionEvaluator> _evaluators;

    public QuestionEvaluationService(
        IEnumerable<IQuestionEvaluator> evaluators)
    {
        _evaluators = evaluators;
    }

   public async Task<QuestionEvaluationResult?> EvaluateAsync(
    StudentAnswerRequest request,
    QuestionType questionType)
{
    var evaluator = _evaluators
        .FirstOrDefault(e => e.SupportedType == questionType);

    if (evaluator is null)
    {
        return null;
    }

    return await evaluator.EvaluateAsync(request);
}
}