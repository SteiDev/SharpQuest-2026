using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Capstone.UI.Views;

/// <summary>
/// A lightweight bar control that renders a rounded horizontal bar
/// proportional to a ratio (0.0 to 1.0).
/// </summary>
public sealed class BarVisual : Control
{
    public static readonly StyledProperty<double> RatioProperty =
        AvaloniaProperty.Register<BarVisual, double>(nameof(Ratio), 0.0);

    public static readonly StyledProperty<IBrush> FillProperty =
        AvaloniaProperty.Register<BarVisual, IBrush>(nameof(Fill), Brushes.Transparent);

    public double Ratio
    {
        get => GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }

    public IBrush Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    static BarVisual()
    {
        AffectsRender<BarVisual>(RatioProperty, FillProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double clamped = Math.Clamp(Ratio, 0.0, 1.0);
        double w = Bounds.Width * clamped;
        if (w > 0)
        {
            context.DrawRectangle(Fill, null, new Rect(0, 0, w, Bounds.Height), 4, 4);
        }
    }
}

