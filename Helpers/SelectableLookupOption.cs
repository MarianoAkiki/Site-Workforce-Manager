using CommunityToolkit.Mvvm.ComponentModel;

namespace Site_Workforce_Manager.Helpers;

public partial class SelectableLookupOption : ObservableObject
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}
