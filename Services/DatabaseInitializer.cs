using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Site_Workforce_Manager.Services;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using (var context = new AppDbContext())
        {
            context.Database.EnsureCreated();
            EnsureTradesSchemaExists(context);
            EnsureDailyRateHistorySchema(context);
            EnsureWorkLogsTableExists(context);
            EnsureWorkerPaymentsTableExists(context);
            EnsureLegacyTradesExist(context);
            BackfillWorkerTrades(context);
            RemoveLegacyWorkerTradeColumn(context);
            RemoveLegacyWorkerNumberColumn(context);
            RemoveLegacyPayrollTables(context);
            MigrateWorkerPaymentsAddWeekStartDate(context);
            MigrateRemoveWorkLogNotes(context);
        }

    }

    private static void EnsureTradesSchemaExists(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS Trades (
                Id INTEGER NOT NULL CONSTRAINT PK_Trades PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """);

        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Trades_Name ON Trades (Name);");

        if (!ColumnExists(context, "Workers", "TradeId"))
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Workers ADD COLUMN TradeId INTEGER NULL;");
        }

        if (!IndexExists(context, "IX_Workers_TradeId"))
        {
            context.Database.ExecuteSqlRaw("CREATE INDEX IX_Workers_TradeId ON Workers (TradeId);");
        }
    }

    private static void EnsureWorkLogsTableExists(AppDbContext context)
    {
        if (TableExists(context, "WorkLogs") &&
            (ColumnExists(context, "WorkLogs", "StartTime") ||
             ColumnExists(context, "WorkLogs", "EndTime") ||
             ColumnExists(context, "WorkLogs", "HourlyRateSnapshot") ||
             ColumnExists(context, "WorkLogs", "PaymentStatus") ||
             !ColumnExists(context, "WorkLogs", "DailyRateSnapshot")))
        {
            var dailyRateExpression = ColumnExists(context, "WorkLogs", "DailyRateSnapshot")
                ? "DailyRateSnapshot"
                : "CAST(HourlyRateSnapshot AS REAL) * 8";

            context.Database.ExecuteSqlRaw(
                """
                CREATE TABLE WorkLogs_New (
                    Id INTEGER NOT NULL CONSTRAINT PK_WorkLogs PRIMARY KEY AUTOINCREMENT,
                    WorkerId INTEGER NOT NULL,
                    ConstructionSiteId INTEGER NOT NULL,
                    WorkDate TEXT NOT NULL,
                    DurationHours TEXT NOT NULL,
                    DailyRateSnapshot TEXT NOT NULL,
                    TotalAmount TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE RESTRICT,
                    FOREIGN KEY (ConstructionSiteId) REFERENCES ConstructionSites (Id) ON DELETE RESTRICT
                );
                """);

            var copyWorkLogsSql = string.Concat(
                """
                INSERT INTO WorkLogs_New (
                    Id,
                    WorkerId,
                    ConstructionSiteId,
                    WorkDate,
                    DurationHours,
                    DailyRateSnapshot,
                    TotalAmount,
                    CreatedAt,
                    UpdatedAt
                )
                SELECT
                    Id,
                    WorkerId,
                    ConstructionSiteId,
                    WorkDate,
                    DurationHours,
                """,
                dailyRateExpression,
                """
                    ,
                    TotalAmount,
                    CreatedAt,
                    UpdatedAt
                FROM WorkLogs;
                """);

            context.Database.ExecuteSqlRaw(copyWorkLogsSql);

            context.Database.ExecuteSqlRaw("DROP TABLE WorkLogs;");
            context.Database.ExecuteSqlRaw("ALTER TABLE WorkLogs_New RENAME TO WorkLogs;");
        }

        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS WorkLogs (
                Id INTEGER NOT NULL CONSTRAINT PK_WorkLogs PRIMARY KEY AUTOINCREMENT,
                WorkerId INTEGER NOT NULL,
                ConstructionSiteId INTEGER NOT NULL,
                WorkDate TEXT NOT NULL,
                DurationHours TEXT NOT NULL,
                DailyRateSnapshot TEXT NOT NULL,
                TotalAmount TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE RESTRICT,
                FOREIGN KEY (ConstructionSiteId) REFERENCES ConstructionSites (Id) ON DELETE RESTRICT
            );
            """);

        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_WorkerId ON WorkLogs (WorkerId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_ConstructionSiteId ON WorkLogs (ConstructionSiteId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkLogs_WorkDate ON WorkLogs (WorkDate);");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_WorkLogs_WorkerId_WorkDate ON WorkLogs (WorkerId, WorkDate);");
    }

    private static void EnsureDailyRateHistorySchema(AppDbContext context)
    {
        if (!TableExists(context, "WorkerRateHistories"))
        {
            return;
        }

        var hasDailyRate = ColumnExists(context, "WorkerRateHistories", "DailyRate");
        var hasHourlyRate = ColumnExists(context, "WorkerRateHistories", "HourlyRate");

        if (hasDailyRate && hasHourlyRate)
        {
            RebuildWorkerRateHistoriesWithoutHourlyRate(context);
            return;
        }

        if (hasDailyRate)
        {
            return;
        }

        if (hasHourlyRate)
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE WorkerRateHistories ADD COLUMN DailyRate TEXT NOT NULL DEFAULT '0';");
            context.Database.ExecuteSqlRaw("UPDATE WorkerRateHistories SET DailyRate = CAST(HourlyRate AS REAL) * 8;");
            RebuildWorkerRateHistoriesWithoutHourlyRate(context);
            return;
        }

        context.Database.ExecuteSqlRaw("ALTER TABLE WorkerRateHistories ADD COLUMN DailyRate TEXT NOT NULL DEFAULT '0';");
    }

    private static void EnsureWorkerPaymentsTableExists(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS WorkerPayments (
                Id INTEGER NOT NULL CONSTRAINT PK_WorkerPayments PRIMARY KEY AUTOINCREMENT,
                WorkerId INTEGER NOT NULL,
                PaymentDate TEXT NOT NULL,
                Amount TEXT NOT NULL,
                WeekStartDate TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE RESTRICT
            );
            """);

        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkerPayments_WorkerId ON WorkerPayments (WorkerId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkerPayments_PaymentDate ON WorkerPayments (PaymentDate);");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_WorkerPayments_WorkerId_WeekStartDate ON WorkerPayments (WorkerId, WeekStartDate);");
    }

    private static void MigrateWorkerPaymentsAddWeekStartDate(AppDbContext context)
    {
        if (!ColumnExists(context, "WorkerPayments", "WeekStartDate"))
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE WorkerPayments ADD COLUMN WeekStartDate TEXT NULL;");
            context.Database.ExecuteSqlRaw("UPDATE WorkerPayments SET WeekStartDate = date(PaymentDate, '-6 days');");
        }

        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkerPayments_WeekStartDate ON WorkerPayments (WeekStartDate);");

        if (ColumnExists(context, "WorkerPayments", "Notes"))
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE WorkerPayments DROP COLUMN Notes;");
        }
    }

    private static void MigrateRemoveWorkLogNotes(AppDbContext context)
    {
        if (ColumnExists(context, "WorkLogs", "Notes"))
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE WorkLogs DROP COLUMN Notes;");
        }
    }

    private static void RebuildWorkerRateHistoriesWithoutHourlyRate(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE WorkerRateHistories_New (
                Id INTEGER NOT NULL CONSTRAINT PK_WorkerRateHistories PRIMARY KEY AUTOINCREMENT,
                WorkerId INTEGER NOT NULL,
                DailyRate TEXT NOT NULL,
                EffectiveFrom TEXT NOT NULL,
                EffectiveTo TEXT NULL,
                FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE CASCADE
            );
            """);

        context.Database.ExecuteSqlRaw(
            """
            INSERT INTO WorkerRateHistories_New (
                Id,
                WorkerId,
                DailyRate,
                EffectiveFrom,
                EffectiveTo
            )
            SELECT
                Id,
                WorkerId,
                DailyRate,
                EffectiveFrom,
                EffectiveTo
            FROM WorkerRateHistories;
            """);

        context.Database.ExecuteSqlRaw("DROP TABLE WorkerRateHistories;");
        context.Database.ExecuteSqlRaw("ALTER TABLE WorkerRateHistories_New RENAME TO WorkerRateHistories;");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_WorkerRateHistories_WorkerId ON WorkerRateHistories (WorkerId);");
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
            CreateSampleWorkLog(context, workers[0].Id, constructionSites[0].Id, new DateTime(2025, 4, 15), 8m),
            CreateSampleWorkLog(context, workers[1].Id, constructionSites[0].Id, new DateTime(2025, 4, 16), 8m),
            CreateSampleWorkLog(context, workers[2].Id, constructionSites[1].Id, new DateTime(2025, 4, 17), 5.5m)
        };

        context.WorkLogs.AddRange(sampleLogs);
        context.SaveChanges();
    }

    private static WorkLog CreateSampleWorkLog(
        AppDbContext context,
        int workerId,
        int constructionSiteId,
        DateTime workDate,
        decimal durationHours)
    {
        var dailyRate = GetDailyRateForDate(context, workerId, workDate);
        var now = DateTime.Now;

        return new WorkLog
        {
            WorkerId = workerId,
            ConstructionSiteId = constructionSiteId,
            WorkDate = workDate,
            DurationHours = Math.Round(durationHours, 2),
            DailyRateSnapshot = dailyRate,
            TotalAmount = Math.Round(durationHours * (dailyRate / 8m), 2),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static decimal GetDailyRateForDate(AppDbContext context, int workerId, DateTime workDate)
    {
        var rate = context.WorkerRateHistories
            .Where(item => item.WorkerId == workerId &&
                           item.EffectiveFrom <= workDate &&
                           (item.EffectiveTo == null || item.EffectiveTo >= workDate))
            .OrderByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        return rate?.DailyRate ?? 0m;
    }

    private static bool TableExists(AppDbContext context, string tableName)
    {
        using var connection = new SqliteConnection(context.Database.GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    private static void EnsureLegacyTradesExist(AppDbContext context)
    {
        if (!ColumnExists(context, "Workers", "Trade"))
        {
            return;
        }

        var now = DateTime.Now;
        var legacyTradeNames = ReadLegacyWorkerTrades(context)
            .Values
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var legacyTradeName in legacyTradeNames)
        {
            var tradeExists = context.Trades.Any(trade => trade.Name.ToLower() == legacyTradeName.ToLower());

            if (tradeExists)
            {
                continue;
            }

            context.Trades.Add(new Trade
            {
                Name = legacyTradeName,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        context.SaveChanges();
    }

    private static void BackfillWorkerTrades(AppDbContext context)
    {
        var tradesByName = context.Trades
            .AsNoTracking()
            .ToDictionary(trade => trade.Name, trade => trade.Id, StringComparer.OrdinalIgnoreCase);

        var workersWithoutTrade = context.Workers
            .Where(worker => worker.TradeId == null)
            .ToList();

        if (workersWithoutTrade.Count == 0 || !ColumnExists(context, "Workers", "Trade"))
        {
            return;
        }

        var legacyTradeNames = ReadLegacyWorkerTrades(context);

        foreach (var worker in workersWithoutTrade)
        {
            if (!legacyTradeNames.TryGetValue(worker.Id, out var legacyTradeName) ||
                string.IsNullOrWhiteSpace(legacyTradeName))
            {
                continue;
            }

            if (tradesByName.TryGetValue(legacyTradeName.Trim(), out var tradeId))
            {
                worker.TradeId = tradeId;
            }
        }

        context.SaveChanges();
    }

    private static void RemoveLegacyWorkerTradeColumn(AppDbContext context)
    {
        if (!ColumnExists(context, "Workers", "Trade"))
        {
            return;
        }

        context.Database.ExecuteSqlRaw("ALTER TABLE Workers DROP COLUMN Trade;");
    }

    private static void RemoveLegacyWorkerNumberColumn(AppDbContext context)
    {
        if (!ColumnExists(context, "Workers", "WorkerNumber"))
        {
            return;
        }

        if (IndexExists(context, "IX_Workers_WorkerNumber"))
        {
            context.Database.ExecuteSqlRaw("DROP INDEX IX_Workers_WorkerNumber;");
        }

        context.Database.ExecuteSqlRaw("ALTER TABLE Workers DROP COLUMN WorkerNumber;");
    }

    private static void RemoveLegacyPayrollTables(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PayrollPayments;");
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PayrollSlipLines;");
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PayrollSlips;");
    }

    private static Dictionary<int, string> ReadLegacyWorkerTrades(AppDbContext context)
    {
        var result = new Dictionary<int, string>();

        using var connection = new SqliteConnection(context.Database.GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Trade FROM Workers WHERE TradeId IS NULL;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var workerId = reader.GetInt32(0);
            var tradeName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            result[workerId] = tradeName;
        }

        return result;
    }

    private static bool ColumnExists(AppDbContext context, string tableName, string columnName)
    {
        using var connection = new SqliteConnection(context.Database.GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IndexExists(AppDbContext context, string indexName)
    {
        using var connection = new SqliteConnection(context.Database.GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name;";
        command.Parameters.AddWithValue("$name", indexName);

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }
}
