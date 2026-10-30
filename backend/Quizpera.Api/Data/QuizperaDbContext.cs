
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Quizpera.Api.Domain.Auth;
using Quizpera.Api.Domain.Questions;
using QuestionDomain = Quizpera.Api.Domain.Questions.Domain;

namespace Quizpera.Api.Data;


public class QuizperaDbContext : IdentityDbContext<ApplicationUser>
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


    public DbSet<ClinicalCase> ClinicalCases => Set<ClinicalCase>();

public DbSet<ClinicalCaseStage> ClinicalCaseStages =>
    Set<ClinicalCaseStage>();

public DbSet<ClinicalCaseQuestion> ClinicalCaseQuestions =>
    Set<ClinicalCaseQuestion>();

public DbSet<ExamSessionCaseProgress> ExamSessionCaseProgress =>
    Set<ExamSessionCaseProgress>();
 
 public DbSet<QuestionRationaleOption> QuestionRationaleOptions
    => Set<QuestionRationaleOption>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExamSessionResponseOption>()
    .HasKey(o => new
    {
        o.ExamSessionResponseId,
        o.QuestionOptionId
    });

modelBuilder.Entity<ExamSessionResponseOption>()
    .HasOne(o => o.ExamSessionResponse)
    .WithMany(r => r.SelectedOptions)
    .HasForeignKey(o => o.ExamSessionResponseId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ExamSessionResponseOption>()
    .HasOne(o => o.QuestionOption)
    .WithMany()
    .HasForeignKey(o => o.QuestionOptionId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExamSessionMatrixAnswer>()
    .HasKey(a => new
    {
        a.ExamSessionResponseId,
        a.MatrixRowId
    });


    modelBuilder.Entity<ExamSessionResponseRationaleOption>()
    .HasKey(r => new
    {
        r.ExamSessionResponseId,
        r.QuestionRationaleOptionId
    });

modelBuilder.Entity<ExamSessionResponseRationaleOption>()
    .HasOne(r => r.ExamSessionResponse)
    .WithMany(response => response.SelectedRationaleOptions)
    .HasForeignKey(r => r.ExamSessionResponseId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ExamSessionResponseRationaleOption>()
    .HasOne(r => r.QuestionRationaleOption)
    .WithMany()
    .HasForeignKey(r => r.QuestionRationaleOptionId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<ExamSessionMatrixAnswer>()
    .HasOne(a => a.ExamSessionResponse)
    .WithMany(r => r.MatrixAnswers)
    .HasForeignKey(a => a.ExamSessionResponseId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<ExamSessionMatrixAnswer>()
    .HasOne(a => a.MatrixRow)
    .WithMany()
    .HasForeignKey(a => a.MatrixRowId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<ExamSessionMatrixAnswer>()
    .HasOne(a => a.SelectedColumn)
    .WithMany()
    .HasForeignKey(a => a.SelectedColumnId)
    .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<ClinicalCaseQuestion>()
    .HasKey(cq => new
    {
        cq.ClinicalCaseId,
        cq.QuestionId,
        cq.ClinicalCaseStageId
    });



    modelBuilder.Entity<ExamSessionCaseProgress>()
    .HasKey(p => new
    {
        p.ExamSessionId,
        p.ClinicalCaseId
    });


        modelBuilder.Entity<ExamSession>()
        .HasOne(s => s.User)
        .WithMany()
        .HasForeignKey(s => s.UserId)
        .OnDelete(DeleteBehavior.Restrict);

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
