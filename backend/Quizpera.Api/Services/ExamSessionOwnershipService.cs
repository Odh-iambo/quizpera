using Microsoft.EntityFrameworkCore;
using Quizpera.Api.Data;

namespace Quizpera.Api.Services;

public class ExamSessionOwnershipService
{
    private readonly QuizperaDbContext _db;

    public ExamSessionOwnershipService(QuizperaDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsOwnerAsync(
        Guid sessionId,
        string userId)
    {
        return await _db.ExamSessions
            .AsNoTracking()
            .AnyAsync(session =>
                session.Id == sessionId &&
                session.UserId == userId);
    }
}
