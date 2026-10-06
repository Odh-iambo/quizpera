using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Domain.Questions;
using QuestionDomain = Quizpera.Api.Domain.Questions.Domain;

namespace Quizpera.Api.Data;


public class QuizperaDbContext : DbContext
{
    public QuizperaDbContext(DbContextOptions<QuizperaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    public DbSet<ExamProgram> ExamPrograms => Set<ExamProgram>();

    public DbSet<QuestionDomain> Domains => Set<QuestionDomain>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<ClientNeed> ClientNeeds => Set<ClientNeed>();

    public DbSet<Exam> Exams => Set<Exam>();

    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ExamQuestion>()
        .HasKey(eq => new { eq.ExamId, eq.QuestionId });
}

}