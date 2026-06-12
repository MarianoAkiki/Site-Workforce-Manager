using System;
using System.Globalization;
using System.Windows.Data;
using Site_Workforce_Manager.Models;

namespace Site_Workforce_Manager.Helpers;

public class EntityStatusToBoolConverter : IValueConverter
{
    public static readonly EntityStatusToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is EntityStatus.Active;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
