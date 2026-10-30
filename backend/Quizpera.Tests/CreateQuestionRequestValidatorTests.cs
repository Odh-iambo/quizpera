using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Validators;

namespace Quizpera.Tests;

public class CreateQuestionRequestValidatorTests
{
    private readonly CreateQuestionRequestValidator _validator = new();





[Theory]
[InlineData(QuestionType.SingleChoice, QuestionScoringMethod.PlusMinus)]
[InlineData(QuestionType.TrueFalse, QuestionScoringMethod.PlusMinus)]
[InlineData(QuestionType.OrderedResponse, QuestionScoringMethod.PlusMinus)]
[InlineData(QuestionType.NumericEntry, QuestionScoringMethod.PlusMinus)]
[InlineData(QuestionType.SingleChoice, QuestionScoringMethod.ZeroOne)]
[InlineData(QuestionType.NumericEntry, QuestionScoringMethod.RationaleScoring)]
public void Validate_UnsupportedScoringMethodForQuestionType_ReturnsScoringMethodError(
    QuestionType questionType,
    QuestionScoringMethod scoringMethod)
{
    var request = CreateRequest(
        questionType,
        correctOptionCount: 1);

    request.Type = questionType;
    request.ScoringMethod = scoringMethod;

    if (questionType == QuestionType.NumericEntry)
    {
        request.NumericCorrectAnswer = 12.5m;
        request.Options = [];
    }

    var errors = _validator.Validate(request);

    Assert.Contains("scoringMethod", errors.Keys);
    Assert.Contains(
        errors["scoringMethod"],
        message => message.Contains("is not supported"));
}

[Fact]
public void Validate_MultipleResponseWithPlusMinus_ReturnsNoErrors()
{
    var request = CreateRequest(
        QuestionType.MultipleResponse,
        correctOptionCount: 2);

    request.ScoringMethod = QuestionScoringMethod.PlusMinus;

    var errors = _validator.Validate(request);

    Assert.DoesNotContain("scoringMethod", errors.Keys);
    Assert.Empty(errors);
}


[Fact]
public void Validate_MultipleResponseWithZeroOne_ReturnsNoErrors()
{
    var request = CreateRequest(
        QuestionType.MultipleResponse,
        correctOptionCount: 2);

    request.ScoringMethod = QuestionScoringMethod.ZeroOne;

    var errors = _validator.Validate(request);

    Assert.DoesNotContain("scoringMethod", errors.Keys);
    Assert.Empty(errors);
}

[Theory]
[InlineData(QuestionType.SingleChoice)]
[InlineData(QuestionType.MultipleResponse)]
[InlineData(QuestionType.TrueFalse)]
[InlineData(QuestionType.OrderedResponse)]
[InlineData(QuestionType.NumericEntry)]
public void Validate_AllOrNothing_IsSupportedForCurrentQuestionTypes(
    QuestionType questionType)
{
    var request = CreateRequest(
        questionType,
        correctOptionCount: 1);

    request.Type = questionType;
    request.ScoringMethod = QuestionScoringMethod.AllOrNothing;

    if (questionType == QuestionType.NumericEntry)
    {
        request.NumericCorrectAnswer = 12.5m;
        request.Options = [];
    }

    if (questionType == QuestionType.OrderedResponse)
    {
        request.Options[0].CorrectOrder = 1;
        request.Options[1].CorrectOrder = 2;
    }

    var errors = _validator.Validate(request);

    Assert.DoesNotContain("scoringMethod", errors.Keys);
}


    [Fact]
    public void Validate_SingleChoiceWithOneCorrectOption_ReturnsNoErrors()
    {
        var request = CreateRequest(
            QuestionType.SingleChoice,
            correctOptionCount: 1);

        var errors = _validator.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Validate_SingleChoiceWithInvalidCorrectOptionCount_ReturnsErrors(
        int correctOptionCount)
    {
        var request = CreateRequest(
            QuestionType.SingleChoice,
            correctOptionCount);

        var errors = _validator.Validate(request);

        Assert.Contains("options", errors.Keys);
        Assert.Contains(
            "A single-choice question must have exactly one correct option.",
            errors["options"]);
    }

    [Fact]
    public void Validate_MultipleResponseWithCorrectOption_ReturnsNoErrors()
    {
        var request = CreateRequest(
            QuestionType.MultipleResponse,
            correctOptionCount: 2);

        var errors = _validator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleResponseWithoutCorrectOptions_ReturnsErrors()
    {
        var request = CreateRequest(
            QuestionType.MultipleResponse,
            correctOptionCount: 0);

        var errors = _validator.Validate(request);

        Assert.Contains("options", errors.Keys);
    }

    [Fact]
    public void Validate_EmptyStem_ReturnsStemError()
    {
        var request = CreateRequest(
            QuestionType.SingleChoice,
            correctOptionCount: 1);

        request.Stem = " ";

        var errors = _validator.Validate(request);

        Assert.Contains("stem", errors.Keys);
        Assert.Contains("Question stem is required.", errors["stem"]);
    }


    [Fact]
public void Validate_UnsupportedQuestionType_ReturnsTypeError()
{
    var request = CreateRequest((QuestionType)999, correctOptionCount: 1);

    var errors = _validator.Validate(request);

    Assert.Contains("type", errors.Keys);
    Assert.Contains(
        "The specified question type is not supported.",
        errors["type"]);
}

[Fact]
public void Validate_MultipleOptionErrors_ReturnsAllMessages()
{
    var request = CreateRequest(
        QuestionType.SingleChoice,
        correctOptionCount: 0);

    request.Options[0].Text = " ";
    request.Options[1].DisplayOrder = request.Options[0].DisplayOrder;

    var errors = _validator.Validate(request);

    Assert.Contains("options", errors.Keys);
    Assert.Contains(
        "A single-choice question must have exactly one correct option.",
        errors["options"]);
    Assert.Contains(
        "Every option must contain text.",
        errors["options"]);
    Assert.Contains(
        "Option display orders must be unique.",
        errors["options"]);
}


[Fact]
public void Validate_TrueFalseWithTwoOptionsAndOneCorrect_ReturnsNoErrors()
{
    var request = CreateTrueFalseRequest(
        optionCount: 2,
        correctOptionCount: 1);

    var errors = _validator.Validate(request);

    Assert.Empty(errors);
}

[Fact]
public void Validate_TrueFalseWithMoreThanTwoOptions_ReturnsErrors()
{
    var request = CreateTrueFalseRequest(
        optionCount: 3,
        correctOptionCount: 1);

    var errors = _validator.Validate(request);

    Assert.Contains("options", errors.Keys);
    Assert.Contains(
        "A true/false question must have exactly two options: True and False.",
        errors["options"]);
}

[Fact]
public void Validate_TrueFalseWithInvalidOptionTexts_ReturnsErrors()
{
    var request = CreateTrueFalseRequest(
        optionCount: 2,
        correctOptionCount: 1);

    request.Options[0].Text = "Yes";
    request.Options[1].Text = "No";

    var errors = _validator.Validate(request);

    Assert.Contains("options", errors.Keys);
    Assert.Contains(
        "A true/false question must have exactly two options: True and False.",
        errors["options"]);
}

[Theory]
[InlineData(0)]
[InlineData(2)]
public void Validate_TrueFalseWithInvalidCorrectOptionCount_ReturnsErrors(
    int correctOptionCount)
{
    var request = CreateTrueFalseRequest(
        optionCount: 2,
        correctOptionCount);

    var errors = _validator.Validate(request);

    Assert.Contains("options", errors.Keys);
    Assert.Contains(
        "A true/false question must have exactly one correct option.",
        errors["options"]);
}

private static CreateQuestionRequest CreateTrueFalseRequest(
    int optionCount,
    int correctOptionCount)
{
    var options = new List<CreateQuestionOptionRequest>
    {
        new()
        {
            Text = "True",
            DisplayOrder = 1,
            IsCorrect = correctOptionCount >= 1
        },
        new()
        {
            Text = "False",
            DisplayOrder = 2,
            IsCorrect = correctOptionCount >= 2
        }
    };

    while (options.Count < optionCount)
    {
        options.Add(new CreateQuestionOptionRequest
        {
            Text = $"Option {options.Count + 1}",
            DisplayOrder = options.Count + 1,
            IsCorrect = false
        });
    }

    return new CreateQuestionRequest
    {
        Type = QuestionType.TrueFalse,
        Stem = "The patient has a fever.",
        Options = options
    };
}

    private static CreateQuestionRequest CreateRequest(
        QuestionType type,
        int correctOptionCount)
    {
        return new CreateQuestionRequest
        {
            Type = type,
            Stem = "Which finding requires nursing intervention?",
            Options =
            [
                new CreateQuestionOptionRequest
                {
                    Text = "Option A",
                    DisplayOrder = 1,
                    IsCorrect = correctOptionCount >= 1
                },
                new CreateQuestionOptionRequest
                {
                    Text = "Option B",
                    DisplayOrder = 2,
                    IsCorrect = correctOptionCount >= 2
                }
            ]
        };

        
    }


[Fact]
public void Validate_NumericEntryWithAnswerAndNoOptions_ReturnsNoErrors()
{
    var request = new CreateQuestionRequest
    {
        Type = QuestionType.NumericEntry,
        Stem = "Calculate the dosage.",
        NumericCorrectAnswer = 12.5m,
        NumericTolerance = 0.5m,
        Options = []
    };

    var errors = _validator.Validate(request);

    Assert.Empty(errors);
}

[Fact]
public void Validate_NumericEntryWithoutCorrectAnswer_ReturnsError()
{
    var request = new CreateQuestionRequest
    {
        Type = QuestionType.NumericEntry,
        Stem = "Calculate the dosage.",
        NumericCorrectAnswer = null,
        Options = []
    };

    var errors = _validator.Validate(request);

    Assert.Contains("numericCorrectAnswer", errors.Keys);
    Assert.Contains(
        "A correct numeric answer is required.",
        errors["numericCorrectAnswer"]);
}

[Fact]
public void Validate_NumericEntryWithNegativeTolerance_ReturnsError()
{
    var request = new CreateQuestionRequest
    {
        Type = QuestionType.NumericEntry,
        Stem = "Calculate the dosage.",
        NumericCorrectAnswer = 12.5m,
        NumericTolerance = -0.5m,
        Options = []
    };

    var errors = _validator.Validate(request);

    Assert.Contains("numericTolerance", errors.Keys);
    Assert.Contains(
        "Numeric tolerance cannot be negative.",
        errors["numericTolerance"]);
}

[Fact]
public void Validate_NumericEntryWithOptions_ReturnsError()
{
    var request = new CreateQuestionRequest
    {
        Type = QuestionType.NumericEntry,
        Stem = "Calculate the dosage.",
        NumericCorrectAnswer = 12.5m,
        Options =
        [
            new CreateQuestionOptionRequest
            {
                Text = "12.5",
                DisplayOrder = 1,
                IsCorrect = true
            }
        ]
    };

    var errors = _validator.Validate(request);

    Assert.Contains("options", errors.Keys);
    Assert.Contains(
        "Numeric Entry questions must not contain selectable options.",
        errors["options"]);
}

    
}
