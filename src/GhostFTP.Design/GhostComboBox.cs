using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GhostFTP.Design;

public sealed class GhostComboBox : ComboBox
{
    public GhostComboBox()
    {
        FontFamily = GhostTheme.UiFont;
        FontSize = 12.5;
        Foreground = GhostTheme.R("Text");
        Background = GhostTheme.R("Surface2");
        BorderBrush = GhostTheme.R("Border");
        BorderThickness = new Thickness(1);
        Padding = new Thickness(10, 6, 36, 6);
        MinHeight = 34;
        VerticalContentAlignment = VerticalAlignment.Center;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Cursor = Cursors.Hand;
        MaxDropDownHeight = 320;
        Template = CreateTemplate();

        Resources[SystemColors.WindowBrushKey] = GhostTheme.R("Surface");
        Resources[SystemColors.WindowTextBrushKey] = GhostTheme.R("Text");
        Resources[SystemColors.ControlBrushKey] = GhostTheme.R("Surface2");
        Resources[SystemColors.ControlTextBrushKey] = GhostTheme.R("Text");
        Resources[SystemColors.HighlightBrushKey] = GhostTheme.R("AccentSoft");
        Resources[SystemColors.HighlightTextBrushKey] = GhostTheme.R("Text");
        Resources[SystemColors.InactiveSelectionHighlightBrushKey] = GhostTheme.R("SurfaceHover");
        Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = GhostTheme.R("Text");
    }

    private static ControlTemplate CreateTemplate()
    {
#pragma warning disable CS0618
        var root = new FrameworkElementFactory(typeof(Grid));

        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, TemplatedParentBinding("Background"));
        border.SetBinding(Border.BorderBrushProperty, TemplatedParentBinding("BorderBrush"));
        border.SetBinding(Border.BorderThicknessProperty, TemplatedParentBinding("BorderThickness"));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);
        root.AppendChild(border);

        var toggle = new FrameworkElementFactory(typeof(ToggleButton));
        toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        toggle.SetValue(Control.BorderBrushProperty, Brushes.Transparent);
        toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });
        toggle.SetValue(Control.TemplateProperty, TransparentToggleTemplate());
        root.AppendChild(toggle);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 36, 0));
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(UIElement.IsHitTestVisibleProperty, false);
        presenter.SetBinding(ContentPresenter.ContentProperty, TemplatedParentBinding("SelectionBoxItem"));
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, TemplatedParentBinding("SelectionBoxItemTemplate"));
        presenter.SetBinding(ContentPresenter.ContentStringFormatProperty, TemplatedParentBinding("SelectionBoxItemStringFormat"));
        root.AppendChild(presenter);

        var arrow = new FrameworkElementFactory(typeof(Path));
        arrow.SetValue(Path.DataProperty, Geometry.Parse("M 0 0 L 5 5 L 10 0 Z"));
        arrow.SetValue(Shape.FillProperty, GhostTheme.R("Muted"));
        arrow.SetValue(FrameworkElement.WidthProperty, 10d);
        arrow.SetValue(FrameworkElement.HeightProperty, 5d);
        arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 13, 0));
        arrow.SetValue(UIElement.IsHitTestVisibleProperty, false);
        root.AppendChild(arrow);

        var popup = new FrameworkElementFactory(typeof(Popup));
        popup.SetValue(FrameworkElement.NameProperty, "PART_Popup");
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.StaysOpenProperty, false);
        popup.SetValue(UIElement.FocusableProperty, false);
        popup.SetBinding(Popup.IsOpenProperty, new Binding("IsDropDownOpen")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });
        popup.SetBinding(Popup.PlacementTargetProperty, new Binding
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.BackgroundProperty, GhostTheme.R("Surface"));
        popupBorder.SetValue(Border.BorderBrushProperty, GhostTheme.R("BorderStrong"));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        popupBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
        popupBorder.SetValue(FrameworkElement.MinWidthProperty, 140d);
        popupBorder.SetValue(FrameworkElement.MaxHeightProperty, 320d);

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scroll.SetValue(Control.PaddingProperty, new Thickness(4));
        var items = new FrameworkElementFactory(typeof(ItemsPresenter));
        scroll.AppendChild(items);
        popupBorder.AppendChild(scroll);
        popup.AppendChild(popupBorder);
        root.AppendChild(popup);

        var template = new ControlTemplate(typeof(GhostComboBox)) { VisualTree = root };
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        template.Triggers.Add(disabled);
        return template;
#pragma warning restore CS0618
    }

    private static ControlTemplate TransparentToggleTemplate()
    {
#pragma warning disable CS0618
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        return new ControlTemplate(typeof(ToggleButton)) { VisualTree = border };
#pragma warning restore CS0618
    }

    private static Binding TemplatedParentBinding(string path) => new(path)
    {
        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
    };
}
