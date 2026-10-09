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

    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();

    public DbSet<ExamSessionResponse> ExamSessionResponses => Set<ExamSessionResponse>();

    public DbSet<ExamSessionQuestionFlag> ExamSessionQuestionFlags
    => Set<ExamSessionQuestionFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ExamQuestion>()
        .HasKey(eq => new { eq.ExamId, eq.QuestionId });

        modelBuilder.Entity<ExamSessionResponse>()
    .HasOne(r => r.ExamSession)
    .WithMany()
    .HasForeignKey(r => r.ExamSessionId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ExamSessionResponse>()
    .HasOne(r => r.Question)
    .WithMany()
    .HasForeignKey(r => r.QuestionId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<ExamSessionResponse>()
    .HasOne(r => r.SelectedOption)
    .WithMany()
    .HasForeignKey(r => r.SelectedOptionId)
    .OnDelete(DeleteBehavior.Restrict);
    
    modelBuilder.Entity<ExamSessionResponse>()
    .HasIndex(r => new { r.ExamSessionId, r.QuestionId })
    .IsUnique();

    modelBuilder.Entity<ExamSessionQuestionFlag>()
    .HasKey(f => new { f.ExamSessionId, f.QuestionId });

    modelBuilder.Entity<ExamSessionQuestionFlag>()
    .HasOne(f => f.ExamSession)
    .WithMany()
    .HasForeignKey(f => f.ExamSessionId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ExamSessionQuestionFlag>()
    .HasOne(f => f.Question)
    .WithMany()
    .HasForeignKey(f => f.QuestionId)
    .OnDelete(DeleteBehavior.Restrict);

}

}