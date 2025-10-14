// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TAOSW.DSC_Decoder.UI
{
    public class BoolToOnOffConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "ON" : "OFF";
            }
            return "OFF";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Equals("ON", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}