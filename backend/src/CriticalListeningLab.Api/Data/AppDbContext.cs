using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<CohortModuleAvailability> CohortModuleAvailabilities => Set<CohortModuleAvailability>();
    public DbSet<AudioSource> AudioSources => Set<AudioSource>();
    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();
    public DbSet<ExerciseSegment> ExerciseSegments => Set<ExerciseSegment>();
    public DbSet<ExerciseLevel> ExerciseLevels => Set<ExerciseLevel>();
    public DbSet<LevelUnlockRequirement> LevelUnlockRequirements => Set<LevelUnlockRequirement>();
    public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();
    public DbSet<TestSession> TestSessions => Set<TestSession>();
    public DbSet<TestSessionQuestion> TestSessionQuestions => Set<TestSessionQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
