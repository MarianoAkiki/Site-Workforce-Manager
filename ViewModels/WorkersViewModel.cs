using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using System.Windows;

namespace Site_Workforce_Manager.ViewModels;

public partial class WorkersViewModel : ObservableObject
{
    public WorkersViewModel()
    {
        NewRateEffectiveDate = DateTime.Today;
        LoadWorkers();
    }

    public ObservableCollection<Worker> Workers { get; } = new();
    public ObservableCollection<WorkerRateHistory> SelectedWorkerRateHistory { get; } = new();

    [ObservableProperty]
    private Worker? selectedWorker;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string trade = string.Empty;

    [ObservableProperty]
    private string newHourlyRate = string.Empty;

    [ObservableProperty]
    private DateTime? newRateEffectiveDate;

    partial void OnSelectedWorkerChanged(Worker? value)
    {
        if (value is null)
        {
            ClearWorkerForm();
            SelectedWorkerRateHistory.Clear();
            return;
        }

        FirstName = value.FirstName;
        LastName = value.LastName;
        Trade = value.Trade;
        LoadRateHistory(value.Id);
    }

    public void LoadWorkers()
    {
        using var context = new AppDbContext();

        var workers = context.Workers
            .AsNoTracking()
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .ToList();

        Workers.Clear();

        foreach (var worker in workers)
        {
            Workers.Add(worker);
        }

        if (SelectedWorker is not null)
        {
            SelectedWorker = Workers.FirstOrDefault(worker => worker.Id == SelectedWorker.Id);
        }
    }

    [RelayCommand]
    private void AddWorker()
    {
        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Trade))
        {
            MessageBox.Show("Please enter first name, last name, and trade.");
            return;
        }

        using var context = new AppDbContext();

        var worker = new Worker
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Trade = Trade.Trim(),
            Status = EntityStatus.Active
        };

        context.Workers.Add(worker);
        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void EditSelectedWorker()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker to edit.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Trade))
        {
            MessageBox.Show("Please enter first name, last name, and trade.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        worker.FirstName = FirstName.Trim();
        worker.LastName = LastName.Trim();
        worker.Trade = Trade.Trim();

        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void DeactivateSelectedWorker()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker to deactivate.");
            return;
        }

        using var context = new AppDbContext();

        var worker = context.Workers.FirstOrDefault(item => item.Id == SelectedWorker.Id);

        if (worker is null)
        {
            MessageBox.Show("The selected worker could not be found.");
            return;
        }

        worker.Status = EntityStatus.Inactive;
        context.SaveChanges();

        LoadWorkers();
        SelectedWorker = Workers.FirstOrDefault(item => item.Id == worker.Id);
    }

    [RelayCommand]
    private void AddHourlyRate()
    {
        if (SelectedWorker is null)
        {
            MessageBox.Show("Please select a worker first.");
            return;
        }

        if (!decimal.TryParse(NewHourlyRate, out var hourlyRate))
        {
            MessageBox.Show("Please enter a valid hourly rate.");
            return;
        }

        if (NewRateEffectiveDate is null)
        {
            MessageBox.Show("Please choose an effective date.");
            return;
        }

        using var context = new AppDbContext();

        var existingOpenRate = context.WorkerRateHistories
            .Where(rate => rate.WorkerId == SelectedWorker.Id && rate.EffectiveTo == null)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .FirstOrDefault();

        if (existingOpenRate is not null && existingOpenRate.EffectiveFrom < NewRateEffectiveDate.Value)
        {
            existingOpenRate.EffectiveTo = NewRateEffectiveDate.Value.AddDays(-1);
        }

        var newRate = new WorkerRateHistory
        {
            WorkerId = SelectedWorker.Id,
            HourlyRate = hourlyRate,
            EffectiveFrom = NewRateEffectiveDate.Value
        };

        context.WorkerRateHistories.Add(newRate);
        context.SaveChanges();

        NewHourlyRate = string.Empty;
        NewRateEffectiveDate = DateTime.Today;
        LoadRateHistory(SelectedWorker.Id);
    }

    private void LoadRateHistory(int workerId)
    {
        using var context = new AppDbContext();

        var rateHistory = context.WorkerRateHistories
            .AsNoTracking()
            .Where(rate => rate.WorkerId == workerId)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ToList();

        SelectedWorkerRateHistory.Clear();

        foreach (var rate in rateHistory)
        {
            SelectedWorkerRateHistory.Add(rate);
        }
    }

    private void ClearWorkerForm()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Trade = string.Empty;
    }
}
