using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;

namespace Site_Workforce_Manager.ViewModels;

public partial class TradesViewModel : ObservableObject
{
    public TradesViewModel()
    {
        LoadTrades();
    }

    public ObservableCollection<Trade> Trades { get; } = new();
    public ObservableCollection<Trade> FilteredTrades { get; } = new();

    [ObservableProperty]
    private Trade? selectedTrade;

    [ObservableProperty]
    private string tradeName = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isTradeFormVisible;

    [ObservableProperty]
    private bool showActiveTrades = true;

    [ObservableProperty]
    private string formTitle = "Add Category";

    [ObservableProperty]
    private string formDescription = "Create a new category for workers.";

    [ObservableProperty]
    private string saveButtonText = "Save Category";

    private int? editingTradeId;

    partial void OnSelectedTradeChanged(Trade? value)
    {
        if (value is null)
        {
            return;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyTradeFilter();
    }

    partial void OnShowActiveTradesChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusFilterButtonText));
        OnPropertyChanged(nameof(StatusFilterLabel));
        ApplyTradeFilter();
    }

    public string StatusFilterButtonText => ShowActiveTrades ? "Show Inactive" : "Show Active";
    public string StatusFilterLabel => ShowActiveTrades ? "Active categories" : "Inactive categories";

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

        ApplyTradeFilter();

        if (SelectedTrade is not null)
        {
            SelectedTrade = FilteredTrades.FirstOrDefault(item => item.Id == SelectedTrade.Id);
        }
    }

    public void ShowListPage()
    {
        IsTradeFormVisible = false;
        editingTradeId = null;
        SelectedTrade = null;
        TradeName = string.Empty;
        Description = string.Empty;
        LoadTrades();
    }

    [RelayCommand]
    private void ToggleStatusFilter()
    {
        ShowActiveTrades = !ShowActiveTrades;
        SelectedTrade = null;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenAddTradeForm()
    {
        editingTradeId = null;
        SelectedTrade = null;
        TradeName = string.Empty;
        Description = string.Empty;
        FormTitle = "Add Category";
        FormDescription = "Create a new category and make it available for worker forms.";
        SaveButtonText = "Save Category";
        IsTradeFormVisible = true;
    }

    [RelayCommand]
    private void OpenEditTradeForm(Trade? trade)
    {
        if (trade is null)
        {
            MessageBox.Show("Please select a category to edit.");
            return;
        }

        editingTradeId = trade.Id;
        SelectedTrade = trade;
        TradeName = trade.Name;
        Description = trade.Description ?? string.Empty;
        FormTitle = "Edit Category";
        FormDescription = "Update the category name or description.";
        SaveButtonText = "Save Changes";
        IsTradeFormVisible = true;
    }

    [RelayCommand]
    private void CancelTradeForm()
    {
        IsTradeFormVisible = false;
        editingTradeId = null;
        TradeName = string.Empty;
        Description = string.Empty;
    }

    [RelayCommand]
    private void SaveTrade()
    {
        if (editingTradeId.HasValue)
        {
            UpdateTrade(editingTradeId.Value);
            return;
        }

        CreateTrade();
    }

    private void CreateTrade()
    {
        if (string.IsNullOrWhiteSpace(TradeName))
        {
            MessageBox.Show("Category name is required.");
            return;
        }

        using var context = new AppDbContext();

        var normalizedName = TradeName.Trim();
        var duplicateExists = context.Trades
            .Any(trade => trade.Name.ToLower() == normalizedName.ToLower());

        if (duplicateExists)
        {
            MessageBox.Show("A category with this name already exists.");
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

        ShowActiveTrades = true;
        LoadTrades();
        SelectedTrade = FilteredTrades.FirstOrDefault(item => item.Id == trade.Id);
        IsTradeFormVisible = false;
    }

    private void UpdateTrade(int tradeId)
    {
        if (string.IsNullOrWhiteSpace(TradeName))
        {
            MessageBox.Show("Category name is required.");
            return;
        }

        using var context = new AppDbContext();

        var normalizedName = TradeName.Trim();
        var duplicateExists = context.Trades
            .Any(trade => trade.Id != tradeId && trade.Name.ToLower() == normalizedName.ToLower());

        if (duplicateExists)
        {
            MessageBox.Show("A category with this name already exists.");
            return;
        }

        var trade = context.Trades.FirstOrDefault(item => item.Id == tradeId);

        if (trade is null)
        {
            MessageBox.Show("The selected category could not be found.");
            return;
        }

        trade.Name = normalizedName;
        trade.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        trade.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        LoadTrades();
        SelectedTrade = FilteredTrades.FirstOrDefault(item => item.Id == trade.Id);
        IsTradeFormVisible = false;
        editingTradeId = null;
    }

    [RelayCommand]
    private void ToggleTradeStatus(Trade? selectedTrade)
    {
        if (selectedTrade is null)
        {
            MessageBox.Show("Please select a category to update.");
            return;
        }

        using var context = new AppDbContext();

        var trade = context.Trades.FirstOrDefault(item => item.Id == selectedTrade.Id);

        if (trade is null)
        {
            MessageBox.Show("The selected category could not be found.");
            return;
        }

        var isDeactivating = trade.IsActive;
        var confirmed = ConfirmationDialogService.Show(
            isDeactivating ? "Deactivate category?" : "Activate category?",
            isDeactivating
                ? $"Are you sure you want to deactivate \"{trade.Name}\"? Existing workers will keep this category, but it will not appear for new worker assignments."
                : $"Are you sure you want to activate \"{trade.Name}\"? It will become available again for worker assignments.",
            isDeactivating ? "Deactivate" : "Activate",
            "Cancel",
            isDeactivating);

        if (!confirmed)
        {
            LoadTrades();
            return;
        }

        trade.IsActive = !trade.IsActive;
        trade.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        var updatedTradeId = trade.Id;
        LoadTrades();
        SelectedTrade = FilteredTrades.FirstOrDefault(item => item.Id == updatedTradeId);
    }

    private void ApplyTradeFilter()
    {
        var search = SearchText.Trim();
        var filteredTrades = Trades
            .Where(trade => trade.IsActive == ShowActiveTrades)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredTrades = filteredTrades.Where(trade =>
                trade.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (trade.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        FilteredTrades.Clear();

        foreach (var trade in filteredTrades.OrderBy(trade => trade.Name))
        {
            FilteredTrades.Add(trade);
        }
    }
}
