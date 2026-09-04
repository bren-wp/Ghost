using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GhostFTP.Design;

public static class GhostTheme
{
    public static readonly FontFamily UiFont = new("Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily DisplayFont = new("Segoe UI Variable Display, Segoe UI");
    public static bool IsDark { get; private set; } = true;

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return true;
        }
    }

    public static void Apply(bool dark)
    {
        IsDark = dark;
        var resources = Application.Current.Resources;
        resources["Bg"] = Brush(dark ? "#0A0C10" : "#F3F5F8");
        resources["Surface"] = Brush(dark ? "#10141B" : "#FFFFFF");
        resources["Surface2"] = Brush(dark ? "#151A23" : "#F7F9FC");
        resources["Surface3"] = Brush(dark ? "#1B2230" : "#EEF2F7");
        resources["SurfaceHover"] = Brush(dark ? "#202938" : "#E8EDF5");
        resources["Text"] = Brush(dark ? "#F7F8FB" : "#151922");
        resources["Muted"] = Brush(dark ? "#98A4B5" : "#667085");
        resources["Subtle"] = Brush(dark ? "#748196" : "#7B8494");
        resources["Border"] = Brush(dark ? "#263043" : "#D8DEE8");
        resources["BorderStrong"] = Brush(dark ? "#344158" : "#C7CFDC");
        resources["Accent"] = Brush("#6F5BFF");
        resources["AccentHover"] = Brush("#806EFF");
        resources["AccentPressed"] = Brush("#5D49EA");
        resources["AccentSoft"] = Brush(dark ? "#28214A" : "#ECE9FF");
        resources["Success"] = Brush("#2EB67D");
        resources["SuccessSoft"] = Brush(dark ? "#15382C" : "#E5F7F0");
        resources["Danger"] = Brush("#E85461");
        resources["DangerSoft"] = Brush(dark ? "#3A1D24" : "#FDECEE");
        resources["Warning"] = Brush("#D99A31");
        resources["WarningSoft"] = Brush(dark ? "#3A2C16" : "#FFF4DE");
        ApplyGlobalStyles(resources);
    }

    public static Brush R(string key) => (Brush)Application.Current.Resources[key];

    public static Border Card(UIElement child, Thickness? padding = null, double radius = 14)
    {
        return new Border
        {
            Background = R("Surface"),
            BorderBrush = R("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Padding = padding ?? new Thickness(16),
            Child = child,
            SnapsToDevicePixels = true
        };
    }

    public static Border Surface(UIElement child, Thickness? padding = null, double radius = 10)
    {
        return new Border
        {
            Background = R("Surface2"),
            BorderBrush = R("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Padding = padding ?? new Thickness(12),
            Child = child
        };
    }

    public static TextBlock Text(string text, double size = 13, bool muted = false, FontWeight? weight = null)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = R(muted ? "Muted" : "Text"),
            FontFamily = UiFont,
            FontSize = size,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    public static TextBlock Caption(string text) => Text(text, 11.5, muted: true, weight: FontWeights.SemiBold);

    public static StackPanel Field(string label, UIElement input, string? hint = null)
    {
        var stack = new StackPanel();
        stack.Children.Add(Caption(label));
        if (input is FrameworkElement element) element.Margin = new Thickness(0, 6, 0, 0);
        stack.Children.Add(input);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            var hintText = Text(hint, 10.5, muted: true);
            hintText.Margin = new Thickness(1, 5, 0, 0);
            stack.Children.Add(hintText);
        }
        return stack;
    }

    public static System.Windows.Controls.Button Button(string text, bool primary = false, bool danger = false, bool subtle = false)
    {
        var foreground = danger || primary ? Brushes.White : R("Text");
        var background = danger ? R("Danger") : primary ? R("Accent") : subtle ? Brushes.Transparent : R("Surface2");
        var border = danger ? R("Danger") : primary ? R("Accent") : subtle ? Brushes.Transparent : R("Border");
        return new System.Windows.Controls.Button
        {
            Content = text,
            FontFamily = UiFont,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = foreground,
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(13, 7, 13, 7),
            MinHeight = 34,
            Cursor = Cursors.Hand,
            Template = RoundedButtonTemplate()
        };
    }

    public static TextBox TextBox(string? text = null)
    {
        var box = new TextBox
        {
            Text = text ?? string.Empty,
            FontFamily = UiFont,
            FontSize = 12.5,
            Foreground = R("Text"),
            Background = R("Surface2"),
            BorderBrush = R("Border"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6, 10, 6),
            MinHeight = 34,
            CaretBrush = R("Text"),
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = true,
            IsTabStop = true,
            IsReadOnly = false,
            AcceptsReturn = false,
            AcceptsTab = false,
            TextWrapping = TextWrapping.NoWrap
        };
        SpellCheck.SetIsEnabled(box, false);
        ConfigureEditableControl(box);
        return box;
    }

    public static PasswordBox PasswordBox()
    {
        var box = new PasswordBox
        {
            FontFamily = UiFont,
            FontSize = 12.5,
            Foreground = R("Text"),
            Background = R("Surface2"),
            BorderBrush = R("Border"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6, 10, 6),
            MinHeight = 34,
            CaretBrush = R("Text"),
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = true,
            IsTabStop = true
        };
        ConfigureEditableControl(box);
        return box;
    }

    public static Border Badge(string text, string backgroundKey = "Surface2", string foregroundKey = "Muted")
    {
        return new Border
        {
            Background = R(backgroundKey),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(9, 4, 9, 4),
            Child = new TextBlock
            {
                Text = text,
                Foreground = R(foregroundKey),
                FontFamily = UiFont,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            }
        };
    }

    private static void ConfigureEditableControl(Control control)
    {
        control.Resources[SystemColors.WindowBrushKey] = R("Surface2");
        control.Resources[SystemColors.WindowTextBrushKey] = R("Text");
        control.Resources[SystemColors.ControlBrushKey] = R("Surface2");
        control.Resources[SystemColors.ControlTextBrushKey] = R("Text");
        control.Resources[SystemColors.HighlightBrushKey] = R("Accent");
        control.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
        control.PreviewMouseLeftButtonDown += (_, _) =>
        {
            if (!control.IsKeyboardFocusWithin)
                _ = control.Focus();
        };
    }

    private static void ApplyGlobalStyles(ResourceDictionary resources)
    {
        var gridHeader = new Style(typeof(GridViewColumnHeader));
        gridHeader.Setters.Add(new Setter(Control.BackgroundProperty, R("Surface2")));
        gridHeader.Setters.Add(new Setter(Control.ForegroundProperty, R("Muted")));
        gridHeader.Setters.Add(new Setter(Control.BorderBrushProperty, R("Border")));
        gridHeader.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        gridHeader.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        gridHeader.Setters.Add(new Setter(Control.FontFamilyProperty, UiFont));
        gridHeader.Setters.Add(new Setter(Control.FontSizeProperty, 11.5));
        gridHeader.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        gridHeader.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        resources[typeof(GridViewColumnHeader)] = gridHeader;

        var listItem = new Style(typeof(ListViewItem));
        listItem.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        listItem.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        listItem.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        listItem.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(7, 4, 7, 4)));
        listItem.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 1, 0, 1)));
        listItem.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, R("SurfaceHover")));
        listItem.Triggers.Add(hover);
        var selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, R("AccentSoft")));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        listItem.Triggers.Add(selected);
        resources[typeof(ListViewItem)] = listItem;

        var listBoxItem = new Style(typeof(ListBoxItem));
        listBoxItem.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        listBoxItem.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        listBoxItem.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
        listBoxItem.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 1, 0, 1)));
        listBoxItem.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        var listHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        listHover.Setters.Add(new Setter(Control.BackgroundProperty, R("SurfaceHover")));
        listBoxItem.Triggers.Add(listHover);
        var listSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        listSelected.Setters.Add(new Setter(Control.BackgroundProperty, R("AccentSoft")));
        listSelected.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        listBoxItem.Triggers.Add(listSelected);
        resources[typeof(ListBoxItem)] = listBoxItem;

        var comboItem = new Style(typeof(ComboBoxItem));
        comboItem.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        comboItem.Setters.Add(new Setter(Control.BackgroundProperty, R("Surface2")));
        comboItem.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 6, 9, 6)));
        var comboHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        comboHover.Setters.Add(new Setter(Control.BackgroundProperty, R("SurfaceHover")));
        comboItem.Triggers.Add(comboHover);
        var comboSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        comboSelected.Setters.Add(new Setter(Control.BackgroundProperty, R("AccentSoft")));
        comboItem.Triggers.Add(comboSelected);
        resources[typeof(ComboBoxItem)] = comboItem;

        var menu = new Style(typeof(ContextMenu));
        menu.Setters.Add(new Setter(Control.BackgroundProperty, R("Surface")));
        menu.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        menu.Setters.Add(new Setter(Control.BorderBrushProperty, R("BorderStrong")));
        menu.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        resources[typeof(ContextMenu)] = menu;

        var menuItem = new Style(typeof(MenuItem));
        menuItem.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        menuItem.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        menuItem.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 16, 7)));
        resources[typeof(MenuItem)] = menuItem;

        var toolTip = new Style(typeof(ToolTip));
        toolTip.Setters.Add(new Setter(Control.BackgroundProperty, R("Surface3")));
        toolTip.Setters.Add(new Setter(Control.ForegroundProperty, R("Text")));
        toolTip.Setters.Add(new Setter(Control.BorderBrushProperty, R("BorderStrong")));
        toolTip.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        toolTip.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));
        resources[typeof(ToolTip)] = toolTip;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static ControlTemplate RoundedButtonTemplate()
    {
#pragma warning disable CS0618
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetBinding(ContentPresenter.MarginProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.AppendChild(presenter);
        var template = new ControlTemplate(typeof(System.Windows.Controls.Button)) { VisualTree = border };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.OpacityProperty, 0.9));
        template.Triggers.Add(hover);
        var pressed = new Trigger { Property = System.Windows.Controls.Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(Control.OpacityProperty, 0.74));
        template.Triggers.Add(pressed);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Control.OpacityProperty, 0.42));
        template.Triggers.Add(disabled);
        return template;
#pragma warning restore CS0618
    }
}
