using System.IO;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.Data;

public class AppDbContext : DbContext
{
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerRateHistory> WorkerRateHistories => Set<WorkerRateHistory>();
    public DbSet<ConstructionSite> ConstructionSites => Set<ConstructionSite>();
    public DbSet<WorkerConstructionSite> WorkerConstructionSites => Set<WorkerConstructionSite>();
    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "siteworkforcemanager.db");
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Worker>(entity =>
        {
            entity.Property(worker => worker.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(worker => worker.LastName).IsRequired().HasMaxLength(100);
            entity.Property(worker => worker.Trade).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<ConstructionSite>(entity =>
        {
            entity.Property(site => site.Name).IsRequired().HasMaxLength(150);
            entity.Property(site => site.Location).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<WorkerRateHistory>(entity =>
        {
            entity.Property(rate => rate.HourlyRate).HasColumnType("decimal(18,2)");

            entity.HasOne(rate => rate.Worker)
                .WithMany(worker => worker.RateHistory)
                .HasForeignKey(rate => rate.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkerConstructionSite>(entity =>
        {
            entity.HasKey(workerSite => new { workerSite.WorkerId, workerSite.ConstructionSiteId });

            entity.HasOne(workerSite => workerSite.Worker)
                .WithMany(worker => worker.WorkerConstructionSites)
                .HasForeignKey(workerSite => workerSite.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(workerSite => workerSite.ConstructionSite)
                .WithMany(site => site.WorkerConstructionSites)
                .HasForeignKey(workerSite => workerSite.ConstructionSiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkLog>(entity =>
        {
            entity.Property(workLog => workLog.DurationHours).HasColumnType("decimal(18,2)");
            entity.Property(workLog => workLog.HourlyRateSnapshot).HasColumnType("decimal(18,2)");
            entity.Property(workLog => workLog.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(workLog => workLog.Notes).HasMaxLength(500);

            entity.HasOne(workLog => workLog.Worker)
                .WithMany(worker => worker.WorkLogs)
                .HasForeignKey(workLog => workLog.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(workLog => workLog.ConstructionSite)
                .WithMany(site => site.WorkLogs)
                .HasForeignKey(workLog => workLog.ConstructionSiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
