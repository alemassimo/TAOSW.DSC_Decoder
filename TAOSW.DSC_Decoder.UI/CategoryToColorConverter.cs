// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.UI
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CategoryOfCall category)
            {
                return category switch
                {
                    CategoryOfCall.Distress => new SolidColorBrush(Color.FromRgb(255, 102, 102)), // Light red
                    CategoryOfCall.Urgency => new SolidColorBrush(Color.FromRgb(255, 165, 0)),   // Orange
                    CategoryOfCall.Safety => new SolidColorBrush(Color.FromRgb(255, 255, 102)),  // Light yellow
                    CategoryOfCall.Routine => new SolidColorBrush(Color.FromRgb(144, 238, 144)), // Light green
                    CategoryOfCall.Error => new SolidColorBrush(Color.FromRgb(255, 182, 193)),   // Light pink
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            
            return new SolidColorBrush(Colors.White);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}