using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class QuestionAuthorizationIntegrationTests : IDisposable
{
    private readonly QuizperaTestFactory _factory;
    private readonly HttpClient _client;

    public QuestionAuthorizationIntegrationTests()
    {
        _factory = new QuizperaTestFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetQuestion_WithoutAuthentication_Returns401()
    {
        var questionId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/questions/{questionId}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetQuestion_AsAuthenticatedStudent_DoesNotReturn401()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/questions/{Guid.NewGuid()}");

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            $"student-{Guid.NewGuid():N}");

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        var response = await _client.SendAsync(request);

        // A nonexistent question should return 404, not 401.
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetQuestion_AsAuthenticatedStudent_ReturnsQuestionWithoutAnswerKey()
    {
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        const string questionStem = "Which action best prevents infection?";
        const string optionText = "Perform hand hygiene";
        const string rationale = "Hand hygiene reduces transmission of pathogens.";

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<QuizperaDbContext>();

            var program = new ExamProgram
            {
                Id = Guid.NewGuid(),
                Name = "Authorization Test Program"
            };

            var domain = new Quizpera.Api.Domain.Questions.Domain
            {
                Id = Guid.NewGuid(),
                Name = "Authorization Test Domain",
                ExamProgramId = program.Id,
                ExamProgram = program
            };

            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                Name = "Authorization Test Subject",
                DomainId = domain.Id,
                Domain = domain
            };

            var question = new Question
            {
                Id = questionId,
                Type = QuestionType.SingleChoice,
                Stem = questionStem,
                ExamProgramId = program.Id,
                ExamProgram = program,
                DomainId = domain.Id,
                Domain = domain,
                SubjectId = subject.Id,
                Subject = subject,
                Difficulty = Difficulty.Easy,
                Rationale = rationale
            };

            var option = new QuestionOption
            {
                Id = optionId,
                QuestionId = question.Id,
                Question = question,
                Text = optionText,
                DisplayOrder = 1,
                IsCorrect = true
            };

            db.AddRange(program, domain, subject, question, option);
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/questions/{questionId}");

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            $"student-{Guid.NewGuid():N}");

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(questionStem, body);
        Assert.Contains(optionText, body);
        Assert.DoesNotContain("isCorrect", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rationale", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rationale, body);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
