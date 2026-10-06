using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Domain.Questions;
using QuestionDomain = Quizpera.Api.Domain.Questions.Domain;

namespace Quizpera.Api.Data.Seed;

public static class QuestionSeed
{
    public static async Task SeedAsync(QuizperaDbContext db)
    {
        if (await db.Questions.AnyAsync())
        {
            return;
        }

        var examProgram = new ExamProgram
        {
            Id = Guid.NewGuid(),
            Name = "NCLEX-RN"
        };

        var domain = new QuestionDomain
        {
            Id = Guid.NewGuid(),
            Name = "Adult Health"
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Cardiovascular",
            Domain = domain
        };

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Name = "Heart Failure",
            Subject = subject
        };

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.SingleChoice,
            Stem = "A client with heart failure reports increasing shortness of breath. Which finding requires the nurse's immediate attention?",
            ExamProgram = examProgram,
            Domain = domain,
            Subject = subject,
            Topic = topic,
            Difficulty = Difficulty.Medium,
            Rationale = "Increasing shortness of breath can indicate worsening heart failure and requires prompt assessment."
        };

        question.Options.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            Text = "Increasing shortness of breath at rest",
            DisplayOrder = 1
        });

        question.Options.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            Text = "Mild fatigue after walking",
            DisplayOrder = 2
        });

        question.Options.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            Text = "Occasional difficulty sleeping",
            DisplayOrder = 3
        });

        question.Options.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            Text = "Decreased appetite",
            DisplayOrder = 4
        });

        db.Questions.Add(question);

        await db.SaveChangesAsync();
    }
}