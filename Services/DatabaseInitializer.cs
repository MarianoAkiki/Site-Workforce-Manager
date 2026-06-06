using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Microsoft.EntityFrameworkCore;

namespace Site_Workforce_Manager.Services;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var context = new AppDbContext();

        context.Database.EnsureCreated();
        EnsureWorkLogsTableExists(context);

        if (context.Workers.Any())
        {
            SeedWorkLogs(context);
            return;
        }

        var workers = new List<Worker>
        {
            new()
            {
                FirstName = "Ahmed",
                LastName = "Hassan",
                Trade = "Electrician",
                Status = EntityStatus.Active
            },
            new()
            {
                FirstName = "John",
                LastName = "Miller",
                Trade = "Carpenter",
                Status = EntityStatus.Active
            },
            new()
            {
                FirstName = "Maria",
                LastName = "Santos",
                Trade = "Plumber",
                Status = EntityStatus.Active
            }
        };

        var constructionSites = new List<ConstructionSite>
        {
            new()
            {
                Name = "Downtown Tower",
                Location = "Beirut Central District",
                Status = EntityStatus.Active
            },
            new()
            {
                Name = "Harbor Residences",
                Location = "Beirut Waterfront",
                Status = EntityStatus.Active
            }
        };

        context.Workers.AddRange(workers);
        context.ConstructionSites.AddRange(constructionSites);
        context.SaveChanges();

        var rateHistories = new List<WorkerRateHistory>
        {
            new()
            {
                WorkerId = workers[0].Id,
                HourlyRate = 18.50m,
                EffectiveFrom = new DateTime(2025, 1, 1)
            },
            new()
            {
                WorkerId = workers[1].Id,
                HourlyRate = 16.75m,
                EffectiveFrom = new DateTime(2025, 2, 1)
            },
            new()
            {
                WorkerId = workers[2].Id,
                HourlyRate = 17.25m,
                EffectiveFrom = new DateTime(2025, 3, 1)
            }
        };

        var workerSiteAssignments = new List<WorkerConstructionSite>
        {
            new()
            {
                WorkerId = workers[0].Id,
                ConstructionSiteId = constructionSites[0].Id,
                AssignedDate = new DateTime(2025, 4, 1),
                Status = EntityStatus.Active
            },
            new()
            {
                WorkerId = workers[1].Id,
                ConstructionSiteId = constructionSites[0].Id,
                AssignedDate = new DateTime(2025, 4, 5),
                Status = EntityStatus.Active
            },
            new()
            {
                WorkerId = workers[2].Id,
                ConstructionSiteId = constructionSites[1].Id,
                AssignedDate = new DateTime(2025, 4, 10),
                Status = EntityStatus.Active
            }
        };

        context.WorkerRateHistories.AddRange(rateHistories);
        context.WorkerConstructionSites.AddRange(workerSiteAssignments);
        context.SaveChanges();

        SeedWorkLogs(context);
    }

    private static void EnsureWorkLogsTableExists(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS WorkLogs (
                Id INTEGER NOT NULL CONSTRAINT PK_WorkLogs PRIMARY KEY AUTOINCREMENT,
                WorkerId INTEGER NOT NULL,
                ConstructionSiteId INTEGER NOT NULL,
                WorkDate TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                DurationHours TEXT NOT NULL,
                HourlyRateSnapshot TEXT NOT NULL,
                TotalAmount TEXT NOT NULL,
                PaymentStatus INTEGER NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE RESTRICT,
                FOREIGN KEY (ConstructionSiteId) REFERENCES ConstructionSites (Id) ON DELETE RESTRICT
            );
            """);

        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_WorkerId ON WorkLogs (WorkerId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_ConstructionSiteId ON WorkLogs (ConstructionSiteId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_WorkDate ON WorkLogs (WorkDate);");
    }

    private static void SeedWorkLogs(AppDbContext context)
    {
        if (context.WorkLogs.Any())
        {
            return;
        }

        var workers = context.Workers
            .OrderBy(worker => worker.Id)
            .ToList();

        var constructionSites = context.ConstructionSites
            .OrderBy(site => site.Id)
            .ToList();

        if (workers.Count < 3 || constructionSites.Count < 2)
        {
            return;
        }

        var sampleLogs = new List<WorkLog>
        {
            CreateSampleWorkLog(context, workers[0].Id, constructionSites[0].Id, new DateTime(2025, 4, 15), new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0), PaymentStatus.Unpaid, "Electrical setup for level 2."),
            CreateSampleWorkLog(context, workers[1].Id, constructionSites[0].Id, new DateTime(2025, 4, 16), new TimeSpan(7, 30, 0), new TimeSpan(15, 30, 0), PaymentStatus.Paid, "Installed interior wood framing."),
            CreateSampleWorkLog(context, workers[2].Id, constructionSites[1].Id, new DateTime(2025, 4, 17), new TimeSpan(9, 0, 0), new TimeSpan(14, 30, 0), PaymentStatus.Cancelled, "Cancelled due to material delivery delay.")
        };

        context.WorkLogs.AddRange(sampleLogs);
        context.SaveChanges();
    }

    private static WorkLog CreateSampleWorkLog(
        AppDbContext context,
        int workerId,
        int constructionSiteId,
        DateTime workDate,
        TimeSpan startTime,
        TimeSpan endTime,
        PaymentStatus paymentStatus,
        string notes)
    {
        var durationHours = Math.Round((decimal)(endTime - startTime).TotalHours, 2);
        var hourlyRate = GetHourlyRateForDate(context, workerId, workDate);
        var now = DateTime.Now;

        return new WorkLog
        {
            WorkerId = workerId,
            ConstructionSiteId = constructionSiteId,
            WorkDate = workDate,
            StartTime = startTime,
            EndTime = endTime,
            DurationHours = durationHours,
            HourlyRateSnapshot = hourlyRate,
            TotalAmount = Math.Round(durationHours * hourlyRate, 2),
            PaymentStatus = paymentStatus,
            Notes = notes,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static decimal GetHourlyRateForDate(AppDbContext context, int workerId, DateTime workDate)
    {
        var rate = context.WorkerRateHistories
            .Where(item => item.WorkerId == workerId &&
                           item.EffectiveFrom <= workDate &&
                           (item.EffectiveTo == null || item.EffectiveTo >= workDate))
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefault();

        return rate?.HourlyRate ?? 0m;
    }
}
