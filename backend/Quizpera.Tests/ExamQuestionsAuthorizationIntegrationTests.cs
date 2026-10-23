using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizpera.Api.Data;
using Quizpera.Api.Domain.Questions;
using Xunit;

namespace Quizpera.Tests;

public sealed class ExamQuestionsAuthorizationIntegrationTests : IDisposable
{
    private readonly QuizperaTestFactory _factory;
    private readonly HttpClient _client;

    public ExamQuestionsAuthorizationIntegrationTests()
    {
        _factory = new QuizperaTestFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetExamQuestions_WithoutAuthentication_Returns401()
    {
        var examId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/exams/{examId}/questions");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetExamQuestions_AsStudent_Returns403()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/exams/{Guid.NewGuid()}/questions");

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            $"student-{Guid.NewGuid():N}");

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Student");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetExamQuestions_AsAdmin_Returns200()
    {
        var programId = Guid.NewGuid();
        var examId = Guid.NewGuid();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<QuizperaDbContext>();

            var program = new ExamProgram
            {
                Id = programId,
                Name = "Authorization Test Program"
            };

            var exam = new Exam
            {
                Id = examId,
                Name = "Authorization Test Exam",
                Type = ExamType.Custom,
                ExamProgramId = programId,
                ExamProgram = program
            };

            db.AddRange(program, exam);
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/exams/{examId}/questions");

        request.Headers.Add(
            TestAuthenticationHandler.UserIdHeader,
            $"admin-{Guid.NewGuid():N}");

        request.Headers.Add(
            TestAuthenticationHandler.RolesHeader,
            "Admin");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("[]", body);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
