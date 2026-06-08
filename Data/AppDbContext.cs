using System.IO;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.Data;

public class AppDbContext : DbContext
{
    public static string GetDatabasePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "siteworkforcemanager.db");
    }

    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerRateHistory> WorkerRateHistories => Set<WorkerRateHistory>();
    public DbSet<ConstructionSite> ConstructionSites => Set<ConstructionSite>();
    public DbSet<WorkerConstructionSite> WorkerConstructionSites => Set<WorkerConstructionSite>();
    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();
    public DbSet<PayrollSlip> PayrollSlips => Set<PayrollSlip>();
    public DbSet<PayrollSlipLine> PayrollSlipLines => Set<PayrollSlipLine>();
    public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Worker>(entity =>
        {
            entity.Property(worker => worker.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(worker => worker.LastName).IsRequired().HasMaxLength(100);

            entity.HasOne(worker => worker.Trade)
                .WithMany(trade => trade.Workers)
                .HasForeignKey(worker => worker.TradeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Trade>(entity =>
        {
            entity.Property(trade => trade.Name).IsRequired().HasMaxLength(100);
            entity.Property(trade => trade.Description).HasMaxLength(250);
            entity.HasIndex(trade => trade.Name).IsUnique();
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

        modelBuilder.Entity<PayrollSlip>(entity =>
        {
            entity.Property(slip => slip.SlipNumber).IsRequired().HasMaxLength(50);
            entity.Property(slip => slip.TotalHours).HasColumnType("decimal(18,2)");
            entity.Property(slip => slip.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(slip => slip.AmountPaid).HasColumnType("decimal(18,2)");
            entity.Property(slip => slip.RemainingBalance).HasColumnType("decimal(18,2)");
            entity.Property(slip => slip.Notes).HasMaxLength(500);
            entity.HasIndex(slip => slip.SlipNumber).IsUnique();

            entity.HasOne(slip => slip.Worker)
                .WithMany(worker => worker.PayrollSlips)
                .HasForeignKey(slip => slip.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollSlipLine>(entity =>
        {
            entity.Property(line => line.WorkerNameSnapshot).IsRequired().HasMaxLength(150);
            entity.Property(line => line.TradeNameSnapshot).HasMaxLength(100);
            entity.Property(line => line.ConstructionSiteNameSnapshot).IsRequired().HasMaxLength(150);
            entity.Property(line => line.DurationHours).HasColumnType("decimal(18,2)");
            entity.Property(line => line.HourlyRateSnapshot).HasColumnType("decimal(18,2)");
            entity.Property(line => line.TotalAmountSnapshot).HasColumnType("decimal(18,2)");
            entity.HasIndex(line => line.WorkLogId).IsUnique();

            entity.HasOne(line => line.PayrollSlip)
                .WithMany(slip => slip.PayrollSlipLines)
                .HasForeignKey(line => line.PayrollSlipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(line => line.WorkLog)
                .WithMany(workLog => workLog.PayrollSlipLines)
                .HasForeignKey(line => line.WorkLogId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollPayment>(entity =>
        {
            entity.Property(payment => payment.Amount).HasColumnType("decimal(18,2)");
            entity.Property(payment => payment.Notes).HasMaxLength(500);

            entity.HasOne(payment => payment.PayrollSlip)
                .WithMany(slip => slip.PayrollPayments)
                .HasForeignKey(payment => payment.PayrollSlipId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
