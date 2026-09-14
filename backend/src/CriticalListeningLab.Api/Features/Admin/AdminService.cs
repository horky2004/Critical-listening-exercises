using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Admin;

public class AdminService(AppDbContext db, IProgressionService progression, TimeProvider time) : IAdminService
{
    public async Task<AdminModuleListResponse> ListModulesAsync(CancellationToken ct)
    {
        var modules = await db.Modules
            .Include(m => m.CohortAvailabilities)
            .ThenInclude(a => a.Cohort)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var items = modules.Select(m => new AdminModuleItem(
            m.Slug,
            m.Name,
            m.IsEnabledGlobally,
            m.CohortAvailabilities
                .OrderBy(a => a.Cohort.Name)
                .Select(a => new AdminCohortOverride(a.CohortId, a.Cohort.Name, a.IsEnabled))
                .ToList())).ToList();

        return new AdminModuleListResponse(items);
    }

    public async Task<UpdateModuleResponse> UpdateModuleAsync(
        string moduleSlug, bool isEnabledGlobally, CancellationToken ct)
    {
        var module = await db.Modules.FirstOrDefaultAsync(m => m.Slug == moduleSlug, ct)
                     ?? throw new NotFoundException("Modul nije pronaden.");

        var previous = module.IsEnabledGlobally;
        module.IsEnabledGlobally = isEnabledGlobally;
        await db.SaveChangesAsync(ct);

        var affected = previous == isEnabledGlobally
            ? 0
            : await CountStudentsInheritingGlobalAsync(module.Id, ct);

        return new UpdateModuleResponse(module.Slug, module.IsEnabledGlobally, affected);
    }

    public async Task<CohortListResponse> ListCohortsAsync(CancellationToken ct)
    {
        var cohorts = await db.Cohorts
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCohortItem(c.Id, c.Name, c.IsActive, c.Users.Count))
            .ToListAsync(ct);

        return new CohortListResponse(cohorts);
    }

    public async Task<AdminCohortItem> CreateCohortAsync(string name, bool isActive, CancellationToken ct)
    {
        var trimmed = ValidateName(name);
        if (await db.Cohorts.AnyAsync(c => c.Name == trimmed, ct))
        {
            throw new ConflictException("Cohort s tim nazivom vec postoji.", "cohort-name-taken");
        }
        if (isActive)
        {
            await DeactivateOthersAsync(null, ct);
        }

        var cohort = new Cohort
        {
            Id = Guid.CreateVersion7(),
            Name = trimmed,
            IsActive = isActive,
            CreatedAt = time.GetUtcNow()
        };
        db.Cohorts.Add(cohort);
        await db.SaveChangesAsync(ct);
        return new AdminCohortItem(cohort.Id, cohort.Name, cohort.IsActive, 0);
    }

    public async Task<AdminCohortItem> UpdateCohortAsync(
        Guid cohortId, string name, bool isActive, CancellationToken ct)
    {
        var cohort = await db.Cohorts.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == cohortId, ct)
                     ?? throw new NotFoundException("Cohort nije pronaden.");

        var trimmed = ValidateName(name);
        if (await db.Cohorts.AnyAsync(c => c.Name == trimmed && c.Id != cohortId, ct))
        {
            throw new ConflictException("Cohort s tim nazivom vec postoji.", "cohort-name-taken");
        }

        cohort.Name = trimmed;
        if (isActive && !cohort.IsActive)
        {
            await DeactivateOthersAsync(cohort.Id, ct);
        }

        cohort.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return new AdminCohortItem(cohort.Id, cohort.Name, cohort.IsActive, cohort.Users.Count);
    }

    public async Task UpdateCohortModuleAsync(
        Guid cohortId, string moduleSlug, bool? isEnabled, CancellationToken ct)
    {
        if (!await db.Cohorts.AnyAsync(c => c.Id == cohortId, ct))
        {
            throw new NotFoundException("Cohort nije pronaden.");
        }

        var module = await db.Modules.FirstOrDefaultAsync(m => m.Slug == moduleSlug, ct)
                     ?? throw new NotFoundException("Modul nije pronaden.");

        var existing = await db.CohortModuleAvailabilities
            .FirstOrDefaultAsync(a => a.CohortId == cohortId && a.ModuleId == module.Id, ct);

        if (isEnabled is null)
        {
            if (existing is not null)
            {
                db.CohortModuleAvailabilities.Remove(existing);
                await db.SaveChangesAsync(ct);
            }

            return;
        }

        if (existing is null)
        {
            db.CohortModuleAvailabilities.Add(new CohortModuleAvailability
            {
                Id = Guid.CreateVersion7(),
                CohortId = cohortId,
                ModuleId = module.Id,
                IsEnabled = isEnabled.Value
            });
        }
        else
        {
            existing.IsEnabled = isEnabled.Value;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<StudentListResponse> ListStudentsAsync(
        Guid? cohortId, string? search, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1)
        {
            throw new BadRequestException("page mora biti >= 1.");
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalLevels = await TotalAssignableLevelsAsync(ct);

        var query = db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Student);

        if (cohortId is not null)
        {
            query = query.Where(u => u.CohortId == cohortId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.Contains(term) || u.DisplayName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var students = await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.CohortId,
                CohortName = u.Cohort == null ? null : u.Cohort.Name,
                u.LastLoginAt,
                Completed = u.Progress.Count(p => p.IsPassed)
            })
            .ToListAsync(ct);

        return new StudentListResponse(
            page,
            pageSize,
            totalCount,
            students.Select(s => new AdminStudentItem(
                s.Id, s.Email, s.DisplayName, s.CohortId, s.CohortName, s.LastLoginAt, s.Completed, totalLevels)).ToList());
    }

    public async Task<StudentProgressResponse> GetStudentProgressAsync(Guid userId, CancellationToken ct)
    {
        var student = await db.Users.AsNoTracking()
            .Include(u => u.Cohort)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
                      ?? throw new NotFoundException("Korisnik nije pronaden.");

        var modules = await db.Modules
            .AsSplitQuery()
            .Include(m => m.AudioSources)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var result = new List<StudentModuleProgress>();
        foreach (var module in modules)
        {
            var sources = new List<StudentSourceProgress>();
            foreach (var source in module.AudioSources.Where(s => s.IsEnabled).OrderBy(s => s.SortOrder))
            {
                var tree = await progression.GetTreeStateAsync(userId, source.Id, ct);
                sources.Add(new StudentSourceProgress(
                    source.Slug,
                    source.Name,
                    tree.Segments.Select(segment => new TreeSegment(
                        segment.Key,
                        segment.Name,
                        segment.Levels.Select(level => new TreeLevel(
                            level.LevelId,
                            level.LevelNumber,
                            level.Title,
                            level.Status,
                            level.BestScore,
                            level.BestScorePercentage,
                            level.QuestionCount,
                            level.PassThreshold,
                            level.AttemptCount,
                            level.FirstPassedAt,
                            level.RequiredLevelIds)).ToList())).ToList()));
            }

            result.Add(new StudentModuleProgress(module.Slug, module.Name, sources));
        }

        return new StudentProgressResponse(
            new AdminStudentRef(student.Id, student.Email, student.DisplayName, student.CohortId, student.Cohort?.Name),
            result);
    }

    public async Task<AdminStudentRef> UpdateStudentAsync(Guid userId, Guid? cohortId, CancellationToken ct)
    {
        var student = await db.Users.Include(u => u.Cohort).FirstOrDefaultAsync(u => u.Id == userId, ct)
                      ?? throw new NotFoundException("Korisnik nije pronaden.");

        if (student.Role != UserRole.Student)
        {
            throw new BadRequestException("Cohort se dodjeljuje samo studentima.");
        }

        if (cohortId is not null && !await db.Cohorts.AnyAsync(c => c.Id == cohortId, ct))
        {
            throw new NotFoundException("Cohort nije pronaden.");
        }

        student.CohortId = cohortId;
        await db.SaveChangesAsync(ct);
        await db.Entry(student).Reference(u => u.Cohort).LoadAsync(ct);

        return new AdminStudentRef(student.Id, student.Email, student.DisplayName, student.CohortId, student.Cohort?.Name);
    }

    private async Task<int> CountStudentsInheritingGlobalAsync(Guid moduleId, CancellationToken ct)
    {
        var lockedCohorts = await db.CohortModuleAvailabilities
            .Where(a => a.ModuleId == moduleId && !a.IsEnabled)
            .Select(a => a.CohortId)
            .ToListAsync(ct);

        return await db.Users.CountAsync(
            u => u.Role == UserRole.Student
                 && (u.CohortId == null || !lockedCohorts.Contains(u.CohortId.Value)),
            ct);
    }

    private async Task<int> TotalAssignableLevelsAsync(CancellationToken ct)
    {
        var modules = await db.Modules
            .AsSplitQuery()
            .Include(m => m.AudioSources)
            .Include(m => m.Segments)
            .ThenInclude(s => s.Levels)
            .ToListAsync(ct);

        return modules.Sum(m =>
            m.Segments.SelectMany(s => s.Levels).Count(l => l.IsEnabled)
            * m.AudioSources.Count(s => s.IsEnabled));
    }

    private async Task DeactivateOthersAsync(Guid? exceptId, CancellationToken ct)
    {
        var others = await db.Cohorts.Where(c => c.IsActive && c.Id != exceptId).ToListAsync(ct);
        foreach (var other in others)
        {
            other.IsActive = false;
        }
    }

    private static string ValidateName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new BadRequestException("Naziv cohorta je obavezan.");
        }

        return trimmed;
    }
}
