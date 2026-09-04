using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace KarzounERP.Helpers;

public static class SimpleColorPicker
{
    public static string? PickColor(string currentHex)
    {
        var color = ToMediaColor(currentHex);
        var previewBrush = new SolidColorBrush(color);

        var window = new Window
        {
            Title = LocalizationManager.Get("CompForm_PickColor"),
            Width = 360,
            Height = 300,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.MainWindow,
            FlowDirection = LocalizationManager.FlowDirection,
            FontFamily = new FontFamily("Segoe UI")
        };

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new Border
        {
            Height = 54,
            CornerRadius = new CornerRadius(6),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Background = previewBrush,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var red = CreateColorSlider("R", color.R);
        var green = CreateColorSlider("G", color.G);
        var blue = CreateColorSlider("B", color.B);
        root.Children.Add(red.Row);
        root.Children.Add(green.Row);
        root.Children.Add(blue.Row);

        void UpdatePreview() => previewBrush.Color = Color.FromRgb(
            (byte)red.Slider.Value,
            (byte)green.Slider.Value,
            (byte)blue.Slider.Value);

        red.Slider.ValueChanged += (_, _) => UpdatePreview();
        green.Slider.ValueChanged += (_, _) => UpdatePreview();
        blue.Slider.ValueChanged += (_, _) => UpdatePreview();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };

        var ok = new Button { Content = LocalizationManager.Get("Btn_Save"), MinWidth = 88 };
        ok.Click += (_, _) => window.DialogResult = true;
        var cancel = new Button
        {
            Content = LocalizationManager.Get("Btn_Cancel"),
            MinWidth = 88,
            Margin = new Thickness(8, 0, 0, 0)
        };
        cancel.Click += (_, _) => window.DialogResult = false;
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        window.Content = root;
        return window.ShowDialog() == true
            ? $"#{previewBrush.Color.R:X2}{previewBrush.Color.G:X2}{previewBrush.Color.B:X2}"
            : null;
    }

    private static (Grid Row, Slider Slider) CreateColorSlider(string label, byte value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });

        var title = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(title, 0);

        var slider = new Slider { Minimum = 0, Maximum = 255, Value = value, TickFrequency = 1, IsSnapToTickEnabled = true };
        Grid.SetColumn(slider, 1);

        var number = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        number.SetBinding(TextBlock.TextProperty, new Binding("Value") { Source = slider, StringFormat = "0" });
        Grid.SetColumn(number, 2);

        grid.Children.Add(title);
        grid.Children.Add(slider);
        grid.Children.Add(number);
        return (grid, slider);
    }

    private static Color ToMediaColor(string? hex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var value = hex.Trim().TrimStart('#');
                if (value.Length == 8)
                    value = value[2..];
                if (value.Length == 6)
                    return Color.FromRgb(
                        Convert.ToByte(value[..2], 16),
                        Convert.ToByte(value.Substring(2, 2), 16),
                        Convert.ToByte(value.Substring(4, 2), 16));
            }
        }
        catch
        {
        }

        return Color.FromRgb(123, 31, 162);
    }
}
