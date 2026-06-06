using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Site_Workforce_Manager.Data;
using Site_Workforce_Manager.Models;
using System.Windows;

namespace Site_Workforce_Manager.ViewModels;

public partial class ConstructionSitesViewModel : ObservableObject
{
    public ConstructionSitesViewModel()
    {
        LoadConstructionSites();
    }

    public ObservableCollection<ConstructionSite> ConstructionSites { get; } = new();

    [ObservableProperty]
    private ConstructionSite? selectedConstructionSite;

    [ObservableProperty]
    private string siteName = string.Empty;

    [ObservableProperty]
    private string location = string.Empty;

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

        if (SelectedConstructionSite is not null)
        {
            SelectedConstructionSite = ConstructionSites.FirstOrDefault(site => site.Id == SelectedConstructionSite.Id);
        }
    }

    [RelayCommand]
    private void AddSite()
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

        LoadConstructionSites();
        SelectedConstructionSite = ConstructionSites.FirstOrDefault(item => item.Id == site.Id);
    }

    [RelayCommand]
    private void EditSelectedSite()
    {
        if (SelectedConstructionSite is null)
        {
            MessageBox.Show("Please select a construction site to edit.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SiteName) || string.IsNullOrWhiteSpace(Location))
        {
            MessageBox.Show("Please enter the site name and location.");
            return;
        }

        using var context = new AppDbContext();

        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == SelectedConstructionSite.Id);

        if (site is null)
        {
            MessageBox.Show("The selected construction site could not be found.");
            return;
        }

        site.Name = SiteName.Trim();
        site.Location = Location.Trim();
        context.SaveChanges();

        LoadConstructionSites();
        SelectedConstructionSite = ConstructionSites.FirstOrDefault(item => item.Id == site.Id);
    }

    [RelayCommand]
    private void DeactivateSelectedSite()
    {
        if (SelectedConstructionSite is null)
        {
            MessageBox.Show("Please select a construction site to deactivate.");
            return;
        }

        using var context = new AppDbContext();

        var site = context.ConstructionSites.FirstOrDefault(item => item.Id == SelectedConstructionSite.Id);

        if (site is null)
        {
            MessageBox.Show("The selected construction site could not be found.");
            return;
        }

        site.Status = EntityStatus.Inactive;
        context.SaveChanges();

        LoadConstructionSites();
        SelectedConstructionSite = ConstructionSites.FirstOrDefault(item => item.Id == site.Id);
    }
}
