using System.Windows.Controls;
using System.Windows.Input;

namespace Site_Workforce_Manager.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private void WorkerSelectionSummary_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpenFilterComboBox(WorkerFilterComboBox);
    }

    private void TradeSelectionSummary_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpenFilterComboBox(TradeFilterComboBox);
    }

    private void ConstructionSiteSelectionSummary_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpenFilterComboBox(ConstructionSiteFilterComboBox);
    }

    private static void OpenFilterComboBox(ComboBox comboBox)
    {
        comboBox.Focus();
        comboBox.IsDropDownOpen = true;
    }
}
