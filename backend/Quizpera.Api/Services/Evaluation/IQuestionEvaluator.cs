using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;

namespace Quizpera.Api.Services.Evaluation;

public interface IQuestionEvaluator
{
    QuestionType SupportedType { get; }

    Task<QuestionEvaluationResult?> EvaluateAsync(
        StudentAnswerRequest request);
}