using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Contracts.Questions;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Quizpera.Api.Services;
using Quizpera.Api.Services.Evaluation;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamSessionResponseServiceTests
{
    private const string TestUserId = "submission-test-user";

    private static QuizperaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizperaDbContext>()
            .UseInMemoryDatabase($"response-tests-{Guid.NewGuid()}")
            .Options;

        return new QuizperaDbContext(options);
    }

    private sealed record SubmissionFixture(
    ExamSession Session,
    Question Question);

private static async Task<SubmissionFixture> CreateFixtureAsync(
    QuizperaDbContext db,
    QuestionType questionType)
{
    var program = new ExamProgram
    {
        Id = Guid.NewGuid(),
        Name = "Submission Test Program"
    };

    var domain = new Quizpera.Api.Domain.Questions.Domain
    {
        Id = Guid.NewGuid(),
        Name = "Submission Test Domain",
        ExamProgramId = program.Id,
        ExamProgram = program
    };

    var subject = new Subject
    {
        Id = Guid.NewGuid(),
        Name = "Submission Test Subject",
        DomainId = domain.Id,
        Domain = domain
    };

    var exam = new Exam
    {
        Id = Guid.NewGuid(),
        Name = "Submission Test Exam",
        Type = ExamType.Custom,
        ExamProgramId = program.Id,
        ExamProgram = program
    };

    var session = new ExamSession
    {
        Id = Guid.NewGuid(),
        ExamId = exam.Id,
        Exam = exam,
        UserId = TestUserId,
        Status = ExamSessionStatus.InProgress,
        StartedAt = DateTime.UtcNow
    };

    var question = new Question
    {
        Id = Guid.NewGuid(),
        Type = questionType,
        Stem = "Submission test question",
        ExamProgramId = program.Id,
        ExamProgram = program,
        DomainId = domain.Id,
        Domain = domain,
        SubjectId = subject.Id,
        Subject = subject,
        Difficulty = Difficulty.Medium,
        MaximumPoints = 1m
    };

    var examQuestion = new ExamQuestion
    {
        ExamId = exam.Id,
        Exam = exam,
        QuestionId = question.Id,
        Question = question,
        DisplayOrder = 1
    };

    db.AddRange(program, domain, subject, exam, session, question, examQuestion);

    await db.SaveChangesAsync();

    return new SubmissionFixture(session, question);
}

[Fact]
public async Task SubmitAnswerAsync_PersistsMatrixAnswers()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.MatrixGrid);

    var rowA = new QuestionMatrixRow
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Airway",
        DisplayOrder = 1
    };

    var rowB = new QuestionMatrixRow
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Breathing",
        DisplayOrder = 2
    };

    var columnA = new QuestionMatrixColumn
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Normal",
        DisplayOrder = 1
    };

    var columnB = new QuestionMatrixColumn
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Abnormal",
        DisplayOrder = 2
    };

    db.QuestionMatrixRows.AddRange(rowA, rowB);
    db.QuestionMatrixColumns.AddRange(columnA, columnB);

    db.QuestionMatrixAnswers.AddRange(
        new QuestionMatrixAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = fixture.Question.Id,
            MatrixRowId = rowA.Id,
            CorrectColumnId = columnA.Id
        },
        new QuestionMatrixAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = fixture.Question.Id,
            MatrixRowId = rowB.Id,
            CorrectColumnId = columnB.Id
        });

    await db.SaveChangesAsync();

    var evaluationService = new QuestionEvaluationService(
        new IQuestionEvaluator[]
        {
            new MatrixGridEvaluator(db)
        });

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var request = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        MatrixAnswers =
        [
            new MatrixAnswerRequest
            {
                MatrixRowId = rowA.Id,
                SelectedColumnId = columnA.Id
            },
            new MatrixAnswerRequest
            {
                MatrixRowId = rowB.Id,
                SelectedColumnId = columnB.Id
            }
        ]
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        request);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.MatrixAnswers)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    Assert.Equal(2, savedResponse.MatrixAnswers.Count);
    Assert.True(savedResponse.IsCorrect);
    Assert.Equal(1m, savedResponse.PointsEarned);

    Assert.Contains(
        savedResponse.MatrixAnswers,
        answer =>
            answer.MatrixRowId == rowA.Id &&
            answer.SelectedColumnId == columnA.Id);

    Assert.Contains(
        savedResponse.MatrixAnswers,
        answer =>
            answer.MatrixRowId == rowB.Id &&
            answer.SelectedColumnId == columnB.Id);
}


[Fact]
public async Task SubmitAnswerAsync_ReplacesPreviousMatrixAnswers()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.MatrixGrid);

    var rowA = new QuestionMatrixRow
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Airway",
        DisplayOrder = 1
    };

    var rowB = new QuestionMatrixRow
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Breathing",
        DisplayOrder = 2
    };

    var columnA = new QuestionMatrixColumn
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Normal",
        DisplayOrder = 1
    };

    var columnB = new QuestionMatrixColumn
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Text = "Abnormal",
        DisplayOrder = 2
    };

    db.QuestionMatrixRows.AddRange(rowA, rowB);
    db.QuestionMatrixColumns.AddRange(columnA, columnB);

    db.QuestionMatrixAnswers.AddRange(
        new QuestionMatrixAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = fixture.Question.Id,
            MatrixRowId = rowA.Id,
            CorrectColumnId = columnA.Id
        },
        new QuestionMatrixAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = fixture.Question.Id,
            MatrixRowId = rowB.Id,
            CorrectColumnId = columnB.Id
        });

    await db.SaveChangesAsync();

    var evaluationService = new QuestionEvaluationService(
        new IQuestionEvaluator[]
        {
            new MatrixGridEvaluator(db)
        });

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var firstRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        MatrixAnswers =
        [
            new MatrixAnswerRequest
            {
                MatrixRowId = rowA.Id,
                SelectedColumnId = columnA.Id
            },
            new MatrixAnswerRequest
            {
                MatrixRowId = rowB.Id,
                SelectedColumnId = columnB.Id
            }
        ]
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        firstRequest);

    var secondRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        MatrixAnswers =
        [
            new MatrixAnswerRequest
            {
                MatrixRowId = rowA.Id,
                SelectedColumnId = columnB.Id
            },
            new MatrixAnswerRequest
            {
                MatrixRowId = rowB.Id,
                SelectedColumnId = columnA.Id
            }
        ]
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        secondRequest);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.MatrixAnswers)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    Assert.Equal(2, savedResponse.MatrixAnswers.Count);
    Assert.False(savedResponse.IsCorrect);
    Assert.Equal(0m, savedResponse.PointsEarned);

    Assert.Contains(
        savedResponse.MatrixAnswers,
        answer =>
            answer.MatrixRowId == rowA.Id &&
            answer.SelectedColumnId == columnB.Id);

    Assert.Contains(
        savedResponse.MatrixAnswers,
        answer =>
            answer.MatrixRowId == rowB.Id &&
            answer.SelectedColumnId == columnA.Id);
}

[Fact]
public async Task SubmitAnswerAsync_PersistsMultipleResponseSelections()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.MultipleResponse);

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option A",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option B",
        DisplayOrder = 2,
        IsCorrect = true
    };

    var optionC = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option C",
        DisplayOrder = 3,
        IsCorrect = false
    };

    db.QuestionOptions.AddRange(optionA, optionB, optionC);
    await db.SaveChangesAsync();

    var evaluators = new IQuestionEvaluator[]
    {
        new MultipleResponseEvaluator(db)
    };

    var evaluationService = new QuestionEvaluationService(evaluators);

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var request = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        SelectedOptionIds = new List<Guid>
        {
            optionA.Id,
            optionB.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        request);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.SelectedOptions)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    Assert.Equal(2, savedResponse.SelectedOptions.Count);

    Assert.Contains(
        savedResponse.SelectedOptions,
        option => option.QuestionOptionId == optionA.Id);

    Assert.Contains(
        savedResponse.SelectedOptions,
        option => option.QuestionOptionId == optionB.Id);

    Assert.All(
        savedResponse.SelectedOptions,
        option => Assert.Null(option.SelectedOrder));
}

[Fact]
public async Task SubmitAnswerAsync_PersistsOrderedResponseSequence()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.OrderedResponse);

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "First action",
        DisplayOrder = 1,
        CorrectOrder = 1
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Second action",
        DisplayOrder = 2,
        CorrectOrder = 2
    };

    var optionC = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Third action",
        DisplayOrder = 3,
        CorrectOrder = 3
    };

    db.QuestionOptions.AddRange(optionA, optionB, optionC);
    await db.SaveChangesAsync();

    var evaluators = new IQuestionEvaluator[]
    {
        new OrderedResponseEvaluator(db)
    };

    var evaluationService = new QuestionEvaluationService(evaluators);

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var request = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        OrderedOptionIds = new List<Guid>
        {
            optionC.Id,
            optionA.Id,
            optionB.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        request);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.SelectedOptions)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    var savedOptions = savedResponse.SelectedOptions
        .OrderBy(option => option.SelectedOrder)
        .ToList();

    Assert.Equal(3, savedOptions.Count);

    Assert.Equal(
        new[] { optionC.Id, optionA.Id, optionB.Id },
        savedOptions.Select(option => option.QuestionOptionId));

    Assert.Equal(
        new int?[] { 1, 2, 3 },
        savedOptions.Select(option => option.SelectedOrder));
}

[Fact]
public async Task SubmitAnswerAsync_ReplacesPreviousMultipleResponseSelections()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.MultipleResponse);

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option A",
        DisplayOrder = 1,
        IsCorrect = true
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option B",
        DisplayOrder = 2,
        IsCorrect = true
    };

    var optionC = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Option C",
        DisplayOrder = 3,
        IsCorrect = false
    };

    db.QuestionOptions.AddRange(optionA, optionB, optionC);
    await db.SaveChangesAsync();

    var evaluationService = new QuestionEvaluationService(
        new IQuestionEvaluator[]
        {
            new MultipleResponseEvaluator(db)
        });

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var firstRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        SelectedOptionIds = new List<Guid>
        {
            optionA.Id,
            optionB.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        firstRequest);

    var secondRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        SelectedOptionIds = new List<Guid>
        {
            optionC.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        secondRequest);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.SelectedOptions)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    var savedOptionIds = savedResponse.SelectedOptions
        .Select(option => option.QuestionOptionId)
        .ToList();

    Assert.Single(savedOptionIds);
    Assert.Equal(optionC.Id, savedOptionIds[0]);
}

[Fact]
public async Task SubmitAnswerAsync_ReplacesPreviousOrderedResponseSequence()
{
    await using var db = CreateDbContext();

    var fixture = await CreateFixtureAsync(
        db,
        QuestionType.OrderedResponse);

    var optionA = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Action A",
        DisplayOrder = 1,
        CorrectOrder = 1
    };

    var optionB = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Action B",
        DisplayOrder = 2,
        CorrectOrder = 2
    };

    var optionC = new QuestionOption
    {
        Id = Guid.NewGuid(),
        QuestionId = fixture.Question.Id,
        Question = fixture.Question,
        Text = "Action C",
        DisplayOrder = 3,
        CorrectOrder = 3
    };

    db.QuestionOptions.AddRange(optionA, optionB, optionC);
    await db.SaveChangesAsync();

    var evaluationService = new QuestionEvaluationService(
        new IQuestionEvaluator[]
        {
            new OrderedResponseEvaluator(db)
        });

    var responseService = new ExamSessionResponseService(
        db,
        evaluationService);

    var firstRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        OrderedOptionIds = new List<Guid>
        {
            optionA.Id,
            optionB.Id,
            optionC.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        firstRequest);

    var secondRequest = new SubmitExamAnswerRequest
    {
        QuestionId = fixture.Question.Id,
        OrderedOptionIds = new List<Guid>
        {
            optionC.Id,
            optionA.Id,
            optionB.Id
        }
    };

    await responseService.SubmitAnswerAsync(
        fixture.Session.Id,
        TestUserId,
        secondRequest);

    var savedResponse = await db.ExamSessionResponses
        .Include(r => r.SelectedOptions)
        .SingleAsync(r =>
            r.ExamSessionId == fixture.Session.Id &&
            r.QuestionId == fixture.Question.Id);

    var savedOptions = savedResponse.SelectedOptions
        .OrderBy(option => option.SelectedOrder)
        .ToList();

    Assert.Equal(3, savedOptions.Count);

    Assert.Equal(
        new[] { optionC.Id, optionA.Id, optionB.Id },
        savedOptions.Select(option => option.QuestionOptionId));

    Assert.Equal(
        new int?[] { 1, 2, 3 },
        savedOptions.Select(option => option.SelectedOrder));
}

}
