using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.ViewModels;

public partial class TradesViewModel : ObservableObject
{
    public TradesViewModel()
    {
        LoadTrades();
    }

    public ObservableCollection<Trade> Trades { get; } = new();

    [ObservableProperty]
    private Trade? selectedTrade;

    [ObservableProperty]
    private string tradeName = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    partial void OnSelectedTradeChanged(Trade? value)
    {
        if (value is null)
        {
            TradeName = string.Empty;
            Description = string.Empty;
            return;
        }

        TradeName = value.Name;
        Description = value.Description ?? string.Empty;
    }

    public void LoadTrades()
    {
        using var context = new AppDbContext();

        var trades = context.Trades
            .AsNoTracking()
            .OrderBy(trade => trade.Name)
            .ToList();

        Trades.Clear();

        foreach (var trade in trades)
        {
            Trades.Add(trade);
        }

        if (SelectedTrade is not null)
        {
            SelectedTrade = Trades.FirstOrDefault(item => item.Id == SelectedTrade.Id);
        }
    }

    [RelayCommand]
    private void AddTrade()
    {
        if (string.IsNullOrWhiteSpace(TradeName))
        {
            MessageBox.Show("Trade name is required.");
            return;
        }

        using var context = new AppDbContext();

        var normalizedName = TradeName.Trim();
        var duplicateExists = context.Trades
            .Any(trade => trade.Name.ToLower() == normalizedName.ToLower());

        if (duplicateExists)
        {
            MessageBox.Show("A trade with this name already exists.");
            return;
        }

        var now = DateTime.Now;
        var trade = new Trade
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Trades.Add(trade);
        context.SaveChanges();

        LoadTrades();
        SelectedTrade = Trades.FirstOrDefault(item => item.Id == trade.Id);
    }

    [RelayCommand]
    private void EditSelectedTrade()
    {
        if (SelectedTrade is null)
        {
            MessageBox.Show("Please select a trade to edit.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TradeName))
        {
            MessageBox.Show("Trade name is required.");
            return;
        }

        using var context = new AppDbContext();

        var normalizedName = TradeName.Trim();
        var duplicateExists = context.Trades
            .Any(trade => trade.Id != SelectedTrade.Id && trade.Name.ToLower() == normalizedName.ToLower());

        if (duplicateExists)
        {
            MessageBox.Show("A trade with this name already exists.");
            return;
        }

        var trade = context.Trades.FirstOrDefault(item => item.Id == SelectedTrade.Id);

        if (trade is null)
        {
            MessageBox.Show("The selected trade could not be found.");
            return;
        }

        trade.Name = normalizedName;
        trade.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        trade.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        LoadTrades();
        SelectedTrade = Trades.FirstOrDefault(item => item.Id == trade.Id);
    }

    [RelayCommand]
    private void DeactivateSelectedTrade()
    {
        if (SelectedTrade is null)
        {
            MessageBox.Show("Please select a trade to deactivate.");
            return;
        }

        using var context = new AppDbContext();

        var trade = context.Trades.FirstOrDefault(item => item.Id == SelectedTrade.Id);

        if (trade is null)
        {
            MessageBox.Show("The selected trade could not be found.");
            return;
        }

        trade.IsActive = false;
        trade.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        LoadTrades();
        SelectedTrade = Trades.FirstOrDefault(item => item.Id == trade.Id);
    }
}
