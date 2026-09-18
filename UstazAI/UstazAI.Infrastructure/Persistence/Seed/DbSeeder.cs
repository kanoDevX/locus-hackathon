using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Infrastructure.Persistence.Seed;

/// <summary>Applies pending migrations and seeds the demo catalog + the judge account documented
/// in the README ("test credentials if login required" per submission rules).</summary>
public static class DbSeeder
{
    public const string JudgeEmail = "judge@ustazai.demo";
    public const string JudgePassword = "JudgePass123!";

    public static async Task SeedAsync(UstazDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Universities.AnyAsync(ct))
        {
            var (universities, _, _, _) = CatalogSeedData.Build();
            db.Universities.AddRange(universities);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Users.AnyAsync(u => u.Email == JudgeEmail, ct))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = JudgeEmail,
                PasswordHash = hasher.Hash(JudgePassword),
                DisplayName = "Hackathon Judge",
                Role = UserRole.Judge
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
