using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Logger.Core.Models;

namespace Logger.Wpf.Converters
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        private static readonly Brush TraceBrush = CreateBrush(148, 163, 184);
        private static readonly Brush SuccessBrush = CreateBrush(74, 222, 128);
        private static readonly Brush FatalBrush = CreateBrush(244, 114, 182);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogLevel level = value is LogLevel typedLevel ? typedLevel : LogLevel.Info;

            switch (level)
            {
                case LogLevel.Trace:
                    return TraceBrush;
                case LogLevel.Debug:
                    return Brushes.LightSkyBlue;
                case LogLevel.Info:
                    return Brushes.LightGreen;
                case LogLevel.Success:
                    return SuccessBrush;
                case LogLevel.Warn:
                    return Brushes.Gold;
                case LogLevel.Error:
                    return Brushes.OrangeRed;
                case LogLevel.Fatal:
                    return FatalBrush;
                default:
                    return Brushes.LightGreen;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static Brush CreateBrush(byte red, byte green, byte blue)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }
    }
}
