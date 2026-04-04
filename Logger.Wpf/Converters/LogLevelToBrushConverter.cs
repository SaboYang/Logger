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
        private static readonly Brush DebugBrush = CreateBrush(96, 165, 250);
        private static readonly Brush InfoBrush = CreateBrush(34, 211, 238);
        private static readonly Brush SuccessBrush = CreateBrush(74, 222, 128);
        private static readonly Brush WarnBrush = CreateBrush(250, 204, 21);
        private static readonly Brush ErrorBrush = CreateBrush(248, 113, 113);
        private static readonly Brush FatalBrush = CreateBrush(244, 63, 94);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogLevel level = value is LogLevel typedLevel ? typedLevel : LogLevel.Info;

            switch (level)
            {
                case LogLevel.Trace:
                    return TraceBrush;
                case LogLevel.Debug:
                    return DebugBrush;
                case LogLevel.Info:
                    return InfoBrush;
                case LogLevel.Success:
                    return SuccessBrush;
                case LogLevel.Warn:
                    return WarnBrush;
                case LogLevel.Error:
                    return ErrorBrush;
                case LogLevel.Fatal:
                    return FatalBrush;
                default:
                    return InfoBrush;
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
