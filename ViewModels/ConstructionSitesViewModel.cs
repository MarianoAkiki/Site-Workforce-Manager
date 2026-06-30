using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Helpers;
using Site_Workforce_Manager.Models;
using Site_Workforce_Manager.Services;
using System.Windows;

namespace Site_Workforce_Manager.ViewModels;

public partial class ConstructionSitesViewModel : ObservableObject
{
    public ConstructionSitesViewModel()
    {
        LoadConstructionSites();
    }

    public ObservableCollection<ConstructionSite> ConstructionSites { get; } = new();
    public ObservableCollection<ConstructionSite> FilteredConstructionSites { get; } = new();
    public PagedList<ConstructionSite> ConstructionSitesPage { get; } = new(25);

    [ObservableProperty]
    private ConstructionSite? selectedConstructionSite;

    [ObservableProperty]
    private string siteName = string.Empty;

    [ObservableProperty]
    private string location = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isSiteFormVisible;

    [ObservableProperty]
    private bool showActiveSites = true;

    [ObservableProperty]
    private string formTitle = "Add Construction Site";

    [ObservableProperty]
    private string formDescription = "Create a construction site and keep site details organized.";

    [ObservableProperty]
    private string saveButtonText = "Save Site";

    private int? editingSiteId;

    partial void OnSelectedConstructionSiteChanged(ConstructionSite? value)
    {
        if (value is null)
        {
            SiteName = string.Empty;
            Location = string.Empty;
            return;
        }

        SiteName = value.Name;
        Location = value.Location;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySiteFilter();
    }

    partial void OnShowActiveSitesChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusFilterButtonText));
        OnPropertyChanged(nameof(StatusFilterLabel));
        ApplySiteFilter();
    }

    public string StatusFilterButtonText => ShowActiveSites ? "Show Inactive" : "Show Active";
    public string StatusFilterLabel => ShowActiveSites ? "Active sites" : "Inactive sites";

    public void LoadConstructionSites()
    {
        using var context = new AppDbContext();

        var sites = context.ConstructionSites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .ToList();

        ConstructionSites.Clear();

        foreach (var site in sites)
        {
            ConstructionSites.Add(site);
        }

        ApplySiteFilter();

        if (SelectedConstructionSite is not null)
        {
            SelectedConstructionSite = FilteredConstructionSites.FirstOrDefault(site => site.Id == SelectedConstructionSite.Id);
        }
    }

    public void ShowListPage()
    {
        IsSiteFormVisible = false;
        editingSiteId = null;
        SelectedConstructionSite = null;
        SiteName = string.Empty;
        Location = string.Empty;
        LoadConstructionSites();
    }

    [RelayCommand]
    private void ToggleStatusFilter()
    {
        ShowActiveSites = !ShowActiveSites;
        SelectedConstructionSite = null;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenAddSiteForm()
    {
        editingSiteId = null;
        SelectedConstructionSite = null;
        SiteName = string.Empty;
        Location = string.Empty;
        FormTitle = "Add Construction Site";
        FormDescription = "Create a construction site and make it available for worker assignments.";
        SaveButtonText = "Save Site";
        IsSiteFormVisible = true;
    }

    [RelayCommand]
    private void OpenEditSiteForm(ConstructionSite? site)
    {
        if (site is null)
        {
            MessageBox.Show("Please select a construction site to edit.");
            return;
        }

        editingSiteId = site.Id;
        SelectedConstructionSite = site;
        SiteName = site.Name;
        Location = site.Location;
        FormTitle = "Edit Construction Site";
        FormDescription = "Update the construction site name or location.";
        SaveButtonText = "Save Changes";
        IsSiteFormVisible = true;
    }

    [RelayCommand]
    private void CancelSiteForm()
    {
        IsSiteFormVisible = false;
        editingSiteId = null;
        SelectedConstructionSite = null;
        SiteName = string.Empty;
        Location = string.Empty;
    }

    [RelayCommand]
    private void SaveSite()
    {
        if (editingSiteId.HasValue)
        {
            UpdateSite(editingSiteId.Value);
            return;
        }

        CreateSite();
    }

    private void CreateSite()
    {
        if (string.IsNullOrWhiteSpace(SiteName) || string.IsNullOrWhiteSpace(Location))
        {
            MessageBox.Show("Please enter the site name and location.");
            return;
        }

        using var context = new AppDbContext();

        var site = new ConstructionSite
        {
            Name = SiteName.Trim(),
            Location = Location.Trim(),
            Status = EntityStatus.Active
        };

        context.ConstructionSites.Add(site);
        context.SaveChanges();

        ShowActiveSites = true;
        LoadConstructionSites();
        SelectedConstructionSite = FilteredConstructionSites.FirstOrDefault(item => item.Id == site.Id);
        IsSiteFormVisible = false;
    }

    private void UpdateSite(int siteId)
    {
        if (string.IsNullOrWhiteSpace(SiteName) || string.IsNullOrWhiteSpace(Location))
        {
            MessageBox.Show("Please enter the site name and location.");
            return;
        }

        using var context = new AppDbContext();

        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == siteId);

        if (site is null)
        {
            MessageBox.Show("The selected construction site could not be found.");
            return;
        }

        site.Name = SiteName.Trim();
        site.Location = Location.Trim();
        context.SaveChanges();

        LoadConstructionSites();
        SelectedConstructionSite = FilteredConstructionSites.FirstOrDefault(item => item.Id == site.Id);
        IsSiteFormVisible = false;
        editingSiteId = null;
    }

    [RelayCommand]
    private void ToggleSiteStatus(ConstructionSite? selectedSite)
    {
        if (selectedSite is null)
        {
            MessageBox.Show("Please select a construction site to update.");
            return;
        }

        using var context = new AppDbContext();

        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == selectedSite.Id);

        if (site is null)
        {
            MessageBox.Show("The selected construction site could not be found.");
            return;
        }

        var isDeactivating = site.Status == EntityStatus.Active;
        var confirmed = ConfirmationDialogService.Show(
            isDeactivating ? "Deactivate construction site?" : "Activate construction site?",
            isDeactivating
                ? $"Are you sure you want to deactivate \"{site.Name}\"? Existing history remains visible, but it will not be available for new assignments."
                : $"Are you sure you want to activate \"{site.Name}\"? It will become available again for assignments.",
            isDeactivating ? "Deactivate" : "Activate",
            "Cancel",
            isDeactivating);

        if (!confirmed)
        {
            LoadConstructionSites();
            return;
        }

        site.Status = isDeactivating ? EntityStatus.Inactive : EntityStatus.Active;
        context.SaveChanges();

        var updatedSiteId = site.Id;
        LoadConstructionSites();
        SelectedConstructionSite = FilteredConstructionSites.FirstOrDefault(item => item.Id == updatedSiteId);
    }

    private void ApplySiteFilter()
    {
        var search = SearchText.Trim();
        var filteredSites = ConstructionSites
            .Where(site => site.Status == (ShowActiveSites ? EntityStatus.Active : EntityStatus.Inactive))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredSites = filteredSites.Where(site =>
                site.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                site.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredConstructionSites.Clear();

        foreach (var site in filteredSites.OrderBy(site => site.Name))
        {
            FilteredConstructionSites.Add(site);
        }

        ConstructionSitesPage.SetSource(FilteredConstructionSites);
    }
}
