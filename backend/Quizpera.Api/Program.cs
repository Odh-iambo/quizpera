using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;
using Quizpera.Api.Data.Seed;
using Quizpera.Api.Validators;
using Quizpera.Api.Services;
using Quizpera.Api.Services.Evaluation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizperaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Quizpera")
    )
);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    
builder.Services.AddScoped<CreateQuestionRequestValidator>();
builder.Services.AddScoped<QuestionValidationService>();
builder.Services.AddOpenApi();

builder.Services.AddScoped<CreateQuestionRequestValidator>();
builder.Services.AddScoped<QuestionValidationService>();

builder.Services.AddScoped<IQuestionEvaluator, SingleChoiceEvaluator>();
builder.Services.AddScoped<QuestionEvaluationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuizperaDbContext>();

    await QuestionSeed.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
