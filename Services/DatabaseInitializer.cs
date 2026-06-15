using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Site_Workforce_Manager.Services;

public static class DatabaseInitializer
{
    private const int FirstWorkerNumber = 1001;

    public static void Initialize()
    {
        using var context = new AppDbContext();

        context.Database.EnsureCreated();
        EnsureWorkerNumberSchemaExists(context);
        EnsureTradesSchemaExists(context);
        EnsureWorkLogsTableExists(context);
        EnsurePayrollTablesExist(context);
        EnsureLegacyTradesExist(context);
        BackfillWorkerTrades(context);
        RemoveLegacyWorkerTradeColumn(context);
    }

    private static void EnsureWorkerNumberSchemaExists(AppDbContext context)
    {
        if (!ColumnExists(context, "Workers", "WorkerNumber"))
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Workers ADD COLUMN WorkerNumber INTEGER NOT NULL DEFAULT 0;");
        }

        BackfillWorkerNumbers(context);

        if (!IndexExists(context, "IX_Workers_WorkerNumber"))
        {
            context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IX_Workers_WorkerNumber ON Workers (WorkerNumber);");
        }
    }

    private static void BackfillWorkerNumbers(AppDbContext context)
    {
        using var connection = new SqliteConnection(context.Database.GetConnectionString());
        connection.Open();

        var nextWorkerNumber = Math.Max(FirstWorkerNumber, GetMaxWorkerNumber(connection) + 1);
        var workerIds = new List<int>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id FROM Workers WHERE WorkerNumber <= 0 ORDER BY Id;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                workerIds.Add(reader.GetInt32(0));
            }
        }

        foreach (var workerId in workerIds)
        {
            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE Workers SET WorkerNumber = $workerNumber WHERE Id = $workerId;";
            updateCommand.Parameters.AddWithValue("$workerNumber", nextWorkerNumber);
            updateCommand.Parameters.AddWithValue("$workerId", workerId);
            updateCommand.ExecuteNonQuery();

            nextWorkerNumber++;
        }
    }

    private static int GetMaxWorkerNumber(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(WorkerNumber), 0) FROM Workers;";

        return Convert.ToInt32(command.ExecuteScalar());
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

    private static void EnsurePayrollTablesExist(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS PayrollSlips (
                Id INTEGER NOT NULL CONSTRAINT PK_PayrollSlips PRIMARY KEY AUTOINCREMENT,
                SlipNumber TEXT NOT NULL,
                WorkerId INTEGER NOT NULL,
                DateFrom TEXT NOT NULL,
                DateTo TEXT NOT NULL,
                TotalHours TEXT NOT NULL,
                TotalAmount TEXT NOT NULL,
                AmountPaid TEXT NOT NULL,
                RemainingBalance TEXT NOT NULL,
                Status INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (WorkerId) REFERENCES Workers (Id) ON DELETE RESTRICT
            );
            """);

        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS PayrollSlipLines (
                Id INTEGER NOT NULL CONSTRAINT PK_PayrollSlipLines PRIMARY KEY AUTOINCREMENT,
                PayrollSlipId INTEGER NOT NULL,
                WorkLogId INTEGER NOT NULL,
                WorkerNameSnapshot TEXT NOT NULL,
                TradeNameSnapshot TEXT NOT NULL DEFAULT '',
                ConstructionSiteNameSnapshot TEXT NOT NULL,
                WorkDate TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                DurationHours TEXT NOT NULL,
                HourlyRateSnapshot TEXT NOT NULL,
                TotalAmountSnapshot TEXT NOT NULL,
                FOREIGN KEY (PayrollSlipId) REFERENCES PayrollSlips (Id) ON DELETE CASCADE,
                FOREIGN KEY (WorkLogId) REFERENCES WorkLogs (Id) ON DELETE RESTRICT
            );
            """);

        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS PayrollPayments (
                Id INTEGER NOT NULL CONSTRAINT PK_PayrollPayments PRIMARY KEY AUTOINCREMENT,
                PayrollSlipId INTEGER NOT NULL,
                PaymentDate TEXT NOT NULL,
                Amount TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (PayrollSlipId) REFERENCES PayrollSlips (Id) ON DELETE CASCADE
            );
            """);

        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PayrollSlips_SlipNumber ON PayrollSlips (SlipNumber);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PayrollSlips_WorkerId ON PayrollSlips (WorkerId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PayrollSlips_DateFrom ON PayrollSlips (DateFrom);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PayrollSlips_DateTo ON PayrollSlips (DateTo);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PayrollSlipLines_PayrollSlipId ON PayrollSlipLines (PayrollSlipId);");
        context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PayrollSlipLines_WorkLogId ON PayrollSlipLines (WorkLogId);");
        context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PayrollPayments_PayrollSlipId ON PayrollPayments (PayrollSlipId);");
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
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        return rate?.HourlyRate ?? 0m;
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
