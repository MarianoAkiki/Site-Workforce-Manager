using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Site_Workforce_Manager.Helpers;

public static class SearchableMultiSelectComboBoxBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SearchableMultiSelectComboBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.RegisterAttached(
            "SearchText",
            typeof(string),
            typeof(SearchableMultiSelectComboBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(SearchableMultiSelectComboBoxBehavior),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static string GetSearchText(DependencyObject obj) => (string)obj.GetValue(SearchTextProperty);
    public static void SetSearchText(DependencyObject obj, string value) => obj.SetValue(SearchTextProperty, value);

    private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            comboBox.IsEditable = true;
            comboBox.IsTextSearchEnabled = false;
            comboBox.StaysOpenOnEdit = true;
            comboBox.Loaded += ComboBoxLoaded;
            comboBox.DropDownOpened += ComboBoxDropDownOpened;
            comboBox.SelectionChanged += ComboBoxSelectionChanged;
        }
        else
        {
            comboBox.Loaded -= ComboBoxLoaded;
            comboBox.DropDownOpened -= ComboBoxDropDownOpened;
            comboBox.SelectionChanged -= ComboBoxSelectionChanged;
            ClearFilter(comboBox);
        }
    }

    private static void ComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            AttachEditableTextBox(comboBox);
        }
    }

    private static void ComboBoxDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        AttachEditableTextBox(comboBox);
        ApplyFilter(comboBox, GetSearchText(comboBox));

        if (GetEditableTextBox(comboBox) is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private static void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || GetIsUpdating(comboBox) || comboBox.SelectedItem is null)
        {
            return;
        }

        SetIsUpdating(comboBox, true);
        comboBox.SelectedItem = null;
        comboBox.IsDropDownOpen = true;
        SetEditableText(comboBox, GetSearchText(comboBox));
        SetIsUpdating(comboBox, false);
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox || GetIsUpdating(comboBox))
        {
            return;
        }

        var searchText = e.NewValue as string ?? string.Empty;
        ApplyFilter(comboBox, searchText);
        SetEditableText(comboBox, searchText);
    }

    private static void AttachEditableTextBox(ComboBox comboBox)
    {
        comboBox.ApplyTemplate();

        if (GetEditableTextBox(comboBox) is not TextBox textBox)
        {
            return;
        }

        textBox.TextChanged -= EditableTextBoxTextChanged;
        textBox.TextChanged += EditableTextBoxTextChanged;
        textBox.GotKeyboardFocus -= EditableTextBoxGotKeyboardFocus;
        textBox.GotKeyboardFocus += EditableTextBoxGotKeyboardFocus;
    }

    private static TextBox? GetEditableTextBox(ComboBox comboBox)
    {
        return comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
    }

    private static void EditableTextBoxGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox comboBox)
        {
            return;
        }

        comboBox.IsDropDownOpen = true;
        ApplyFilter(comboBox, GetSearchText(comboBox));
    }

    private static void EditableTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox comboBox || GetIsUpdating(comboBox))
        {
            return;
        }

        var text = textBox.Text ?? string.Empty;
        SetSearchText(comboBox, text);
        ApplyFilter(comboBox, text);

        if (comboBox.IsKeyboardFocusWithin && !comboBox.IsDropDownOpen)
        {
            comboBox.IsDropDownOpen = true;
        }
    }

    private static void ApplyFilter(ComboBox comboBox, string searchText)
    {
        if (comboBox.ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
        if (view is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            view.Filter = null;
            view.Refresh();
            return;
        }

        var trimmedSearchText = searchText.Trim();
        view.Filter = item => GetDisplayText(comboBox, item).Contains(trimmedSearchText, StringComparison.CurrentCultureIgnoreCase);
        view.Refresh();
    }

    private static void ClearFilter(ComboBox comboBox)
    {
        if (comboBox.ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
        if (view is null)
        {
            return;
        }

        view.Filter = null;
        view.Refresh();
    }

    private static void SetEditableText(ComboBox comboBox, string text)
    {
        if (GetEditableTextBox(comboBox) is not TextBox textBox || textBox.Text == text)
        {
            return;
        }

        SetIsUpdating(comboBox, true);
        textBox.Text = text;
        textBox.CaretIndex = text.Length;
        SetIsUpdating(comboBox, false);
    }

    private static string GetDisplayText(ComboBox comboBox, object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var displayMemberPath = comboBox.DisplayMemberPath;
        if (string.IsNullOrWhiteSpace(displayMemberPath))
        {
            var nameProperty = TypeDescriptor.GetProperties(item)["Name"];
            return nameProperty?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
        }

        var property = TypeDescriptor.GetProperties(item)[displayMemberPath];
        return property?.GetValue(item)?.ToString() ?? string.Empty;
    }
}
