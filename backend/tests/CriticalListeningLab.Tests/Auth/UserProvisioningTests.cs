using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Tests.Auth;

public class UserProvisioningTests
{
    [Fact]
    public async Task First_login_creates_student_and_assigns_active_cohort()
    {
        await using var db = CreateDb();
        var cohort = AddActiveCohort(db);
        var service = CreateService(db);

        var user = await service.GetOrCreateAsync(
            "oid-student", "Ana@Student.Algebra.HR", "Ana Horvat", CancellationToken.None);

        user.Role.ShouldBe(UserRole.Student);
        user.Email.ShouldBe("ana@student.algebra.hr");
        user.DisplayName.ShouldBe("Ana Horvat");
        user.CohortId.ShouldBe(cohort.Id);
        user.EntraObjectId.ShouldBe("oid-student");
    }

    [Fact]
    public async Task First_login_with_admin_email_creates_admin_without_cohort()
    {
        await using var db = CreateDb();
        AddActiveCohort(db);
        var service = CreateService(db, adminEmails: ["nastavnik@algebra.hr"]);

        var user = await service.GetOrCreateAsync(
            "oid-admin", "nastavnik@algebra.hr", "Nastavnik", CancellationToken.None);

        user.Role.ShouldBe(UserRole.Admin);
        user.CohortId.ShouldBeNull();
    }

    [Fact]
    public async Task Student_without_active_cohort_is_created_with_null_cohort()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var user = await service.GetOrCreateAsync(
            "oid-1", "ana@student.algebra.hr", "Ana", CancellationToken.None);

        user.CohortId.ShouldBeNull();
    }

    [Fact]
    public async Task Same_oid_with_changed_email_updates_existing_row()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var first = await service.GetOrCreateAsync(
            "oid-1", "stari@student.algebra.hr", "Staro Ime", CancellationToken.None);

        var second = await service.GetOrCreateAsync(
            "oid-1", "novi@student.algebra.hr", "Novo Ime", CancellationToken.None);

        second.Id.ShouldBe(first.Id);
        second.Email.ShouldBe("novi@student.algebra.hr");
        second.DisplayName.ShouldBe("Novo Ime");
        (await db.Users.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Removing_email_from_AdminEmails_does_not_demote_existing_admin()
    {
        await using var db = CreateDb();
        var service = CreateService(db, adminEmails: ["nastavnik@algebra.hr"]);

        await service.GetOrCreateAsync(
            "oid-admin", "nastavnik@algebra.hr", "Nastavnik", CancellationToken.None);

        var later = CreateService(db, adminEmails: []);
        var again = await later.GetOrCreateAsync(
            "oid-admin", "nastavnik@algebra.hr", "Nastavnik", CancellationToken.None);

        again.Role.ShouldBe(UserRole.Admin);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Cohort AddActiveCohort(AppDbContext db)
    {
        var cohort = new Cohort
        {
            Id = Guid.CreateVersion7(),
            Name = "2025/26",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Cohorts.Add(cohort);
        db.SaveChanges();
        return cohort;
    }

    private static UserProvisioningService CreateService(
        AppDbContext db, string[]? adminEmails = null)
    {
        var options = Options.Create(new AuthOptions
        {
            AdminEmails = adminEmails ?? []
        });

        return new UserProvisioningService(
            db, options, TimeProvider.System, NullLogger<UserProvisioningService>.Instance);
    }
}
