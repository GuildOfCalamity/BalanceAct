using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;
using Windows.UI;

using BalanceAct.Models;
using System.Diagnostics;

namespace BalanceAct.Controls;

/// <summary>
/// Ported from my WPF NOAA CartesianChart control, with some modifications for WinUI3. 
/// For NOAA repo visit https://github.com/GuildOfCalamity/NOAA
/// </summary>
public sealed partial class CartesianChart : UserControl
{
    private bool _constantTooltip = false;
    private bool _animating = false;
    private const double HitThreshold = 22.0; // in pixels

    private long _minX;
    private long _maxX;
    private double _maxY;

    private Storyboard? _dotPulseStoryboard;
    private Storyboard? _tooltipFadeInStoryboard;
    private Storyboard? _tooltipFadeOutStoryboard;
    private DoubleAnimation? _fadeOutTooltipAnimation;

    public List<ChartSeries> Series
    {
        get => (List<ChartSeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series),
            typeof(List<ChartSeries>),
            typeof(CartesianChart),
            new PropertyMetadata(null, OnSeriesChanged));

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CartesianChart chart)
        {
            chart.Redraw();
        }
    }

    public CartesianChart()
    {
        this.InitializeComponent();

        ConfigureFadeOutAnimation();

        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
        Unloaded += (_, _) => Unload();

        if (_constantTooltip)
            PointerMoved += OnPointerMovedConstant;
        else
            PointerMoved += OnPointerMoved;

        PointerExited += OnPointerExited;
    }

    void Redraw()
    {
        if (PART_Canvas == null)
            return;

        if (PART_Canvas.ActualWidth.IsInvalidOrZero() || PART_Canvas.ActualHeight.IsInvalidOrZero())
        {
            Debug.WriteLine($"[WARNING] PART_Canvas has no width or height!");
            //PART_Canvas.Width = PART_Canvas.Height= 500;
        }

        PART_Canvas.Children.Clear();

        if (Series == null || Series.Count == 0)
            return;

        var points = Series[0].Points;
        if (points == null || points.Count < 2)
            return;

        if (!_constantTooltip)
        {
            _minX = points.Min(p => p.Time.Ticks);
            _maxX = points.Max(p => p.Time.Ticks);
            _maxY = points.Max(p => p.Value);
            if (_maxY <= 0)
                _maxY = 1;
        }

        DrawGridlines(points);
        DrawAxes();
        DrawSeries();

        if (Series[0].ShowPoints)
        {
            DrawAllDataPointDots();

            PART_Canvas.Children.Remove(PART_PointLayer);
            PART_Canvas.Children.Add(PART_PointLayer);
        }

        PART_Canvas.Children.Remove(PART_HighlightDot);
        PART_Canvas.Children.Add(PART_HighlightDot);

        PART_Canvas.Children.Remove(PART_Tooltip);
        PART_Canvas.Children.Add(PART_Tooltip);
    }

    void DrawAxes()
    {
        double left = 40;
        double bottom = ActualHeight - 30;
        double top = 10;
        double right = ActualWidth - 10;

        var axisBrush = new SolidColorBrush(Colors.Gray);

        var xAxis = new Line
        {
            X1 = left,
            Y1 = bottom,
            X2 = right,
            Y2 = bottom,
            Stroke = axisBrush,
            StrokeThickness = 1
        };

        var yAxis = new Line
        {
            X1 = left,
            Y1 = top,
            X2 = left,
            Y2 = bottom,
            Stroke = axisBrush,
            StrokeThickness = 1
        };

        PART_Canvas.Children.Add(xAxis);
        PART_Canvas.Children.Add(yAxis);
    }

    void DrawSeries()
    {
        bool smoothJoin = true; // kept for parity, though Path uses straight segments here

        foreach (var series in Series)
        {
            if (series.Points == null || series.Points.Count < 2)
                continue;

            long minX = series.Points.Min(p => p.Time.Ticks);
            long maxX = series.Points.Max(p => p.Time.Ticks);
            double maxY = series.Points.Max(p => p.Value);
            if (maxY <= 0)
                maxY = 1;

            double PlotX(DateTime t) =>
                40 + (ActualWidth - 50) * ((t.Ticks - minX) / (double)(maxX - minX));

            double PlotY(double v) =>
                (ActualHeight - 30) - (ActualHeight - 40) * (v / maxY);

            var figure = new PathFigure
            {
                IsClosed = false,
                IsFilled = false,
                StartPoint = new Point(PlotX(series.Points[0].Time),
                                       PlotY(series.Points[0].Value))
            };

            foreach (var p in series.Points.Skip(1))
            {
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(PlotX(p.Time), PlotY(p.Value))
                });
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            var path = new Path
            {
                Data = geometry,
                Stroke = series.Stroke ?? new SolidColorBrush(Colors.DeepSkyBlue),
                StrokeThickness = series.StrokeThickness,
                StrokeLineJoin = smoothJoin ? PenLineJoin.Round : PenLineJoin.Miter
            };

            PART_Canvas.Children.Add(path);
        }
    }

    void DrawGridlines(List<ChartPoint> points)
    {
        double left = 40;
        double bottom = ActualHeight - 30;
        double top = 10;
        double right = ActualWidth - 10;

        long minX = points.Min(p => p.Time.Ticks);
        long maxX = points.Max(p => p.Time.Ticks);
        double maxY = points.Max(p => p.Value);
        if (maxY <= 0)
            maxY = 1;

        int xSteps = 6;
        int ySteps = 6;

        Brush gridBrush;
        double gridThickness;

        if (Series != null && Series.Count > 0 && Series[0].GridPen != null)
        {
            gridBrush = Series[0].GridPen;
            gridThickness = Series[0].GridThickness;
        }
        else
        {
            gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            gridThickness = 1;
        }

        // Horizontal gridlines + Y labels
        for (int i = 0; i <= ySteps; i++)
        {
            double yVal = (maxY / ySteps) * i;
            double y = bottom - (bottom - top) * (yVal / maxY);

            var line = new Line
            {
                X1 = left,
                Y1 = y,
                X2 = right,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = gridThickness
            };
            PART_Canvas.Children.Add(line);

            var label = new TextBlock
            {
                Text = $"{yVal:0.0}",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 12
            };

            Canvas.SetLeft(label, 5);
            Canvas.SetTop(label, y - 8);
            PART_Canvas.Children.Add(label);
        }

        // Vertical gridlines + X labels
        for (int i = 0; i <= xSteps; i++)
        {
            long tick = minX + (long)((maxX - minX) * (i / (double)xSteps));
            DateTime t = new DateTime(tick);
            double x = left + (right - left) * (i / (double)xSteps);

            var line = new Line
            {
                X1 = x,
                Y1 = top,
                X2 = x,
                Y2 = bottom,
                Stroke = gridBrush,
                StrokeThickness = gridThickness
            };
            PART_Canvas.Children.Add(line);

            var label = new TextBlock
            {
                Text = t.ToString("MM/dd\nHH:mm"),
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 11,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(label, x - 25);
            Canvas.SetTop(label, bottom + 5);
            PART_Canvas.Children.Add(label);
        }
    }

    void DrawAllDataPointDots()
    {
        PART_PointLayer.Children.Clear();

        if (Series == null || Series.Count == 0)
            return;

        var series = Series[0];
        if (series.Points == null)
            return;

        foreach (var p in series.Points)
        {
            double x = PlotX_Scoped(p.Time);
            double y = PlotY_Scoped(p.Value);

            var dot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = new SolidColorBrush(Colors.DeepSkyBlue),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(dot, x - dot.Width / 2);
            Canvas.SetTop(dot, y - dot.Height / 2);

            PART_PointLayer.Children.Add(dot);
        }
    }

    void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (Series == null || Series.Count == 0)
            return;

        var pos = e.GetCurrentPoint(PART_Canvas).Position;
        var series = Series[0];
        if (series.Points == null || series.Points.Count < 2)
            return;

        // Old version that only considered X distance, which caused issues with stacked series where multiple points share the same DateTime (X value).
        //var closest = series.Points
        //    .OrderBy(p => Math.Abs(PlotX_Scoped(p.Time) - pos.X))
        //    .FirstOrDefault();

        // Account for stacked series by comparing distance to the actual point, not just the line (items with same DateTime)
        var closest = series.Points
            .OrderBy(p =>
            {
                double dx = PlotX_Scoped(p.Time) - pos.X;
                double dy = PlotY_Scoped(p.Value) - pos.Y;
                return dx * dx + dy * dy; // squared distance (faster)
            })
            .First();


        if (closest == null)
            return;


        // Account for stacked series by comparing distance to the actual point, not just the line (items with same DateTime)
        double xs = PlotX_Scoped(closest.Time);
        double ys = PlotY_Scoped(closest.Value);
        double dx = Math.Abs(xs - pos.X);
        double dy = Math.Abs(ys - pos.Y);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= HitThreshold)
        {
            double x = PlotX_Scoped(closest.Time);
            double y = PlotY_Scoped(closest.Value);

            Canvas.SetLeft(PART_HighlightDot, x - PART_HighlightDot.Width / 2);
            Canvas.SetTop(PART_HighlightDot, y - PART_HighlightDot.Height / 2);

            if (PART_HighlightDot.Visibility != Visibility.Visible)
            {
                PART_HighlightDot.Visibility = Visibility.Visible;
                StartDotPulse();
            }

            double tooltipWidth = PART_Tooltip.ActualWidth;
            double tooltipHeight = PART_Tooltip.ActualHeight;

            // Calculate tooltip offset to keep it within bounds
            double offsetX = 20;
            if (pos.X + tooltipWidth + offsetX > ActualWidth)
                offsetX = -tooltipWidth - 10;

            Canvas.SetLeft(PART_Tooltip, pos.X + offsetX);
            Canvas.SetTop(PART_Tooltip, pos.Y - tooltipHeight + 16);

            PART_TooltipText.Text = $"{closest.Time:ddd, dd MMM yyyy}\n{closest.Uom}{closest.Value:0.00}\n{closest.Info}";

            if (PART_Tooltip.Visibility != Visibility.Visible)
            {
                PART_Tooltip.Visibility = Visibility.Visible;
                PART_Tooltip.Opacity = 0.0;
                FadeInTooltip();
            }
        }
        else
        {
            Debug.WriteLine($"[DEBUG] dy {dy:0.0} is not within threshold {HitThreshold:0.0}");

            if (PART_HighlightDot.Visibility == Visibility.Visible)
            {
                StopDotPulse();
                PART_HighlightDot.Visibility = Visibility.Collapsed;
            }

            if (PART_Tooltip.Visibility == Visibility.Visible)
            {
                if (!_animating)
                    FadeOutTooltip();
            }
        }
    }

    void OnPointerMovedConstant(object sender, PointerRoutedEventArgs e)
    {
        if (Series == null || Series.Count == 0)
            return;

        var pos = e.GetCurrentPoint(this).Position;
        var series = Series[0];
        if (series.Points == null || series.Points.Count == 0)
            return;

        long minTicks = series.Points.Min(x => x.Time.Ticks);
        long maxTicks = series.Points.Max(x => x.Time.Ticks);

        var closest = series.Points
            .OrderBy(p =>
            {
                double xPlot = 40 + (ActualWidth - 50) *
                    ((p.Time.Ticks - minTicks) / (double)(maxTicks - minTicks));
                return Math.Abs(xPlot - pos.X);
            })
            .FirstOrDefault();

        if (closest == null)
            return;

        PART_Tooltip.Visibility = Visibility.Collapsed;
        PART_TooltipText.Text = $"{closest.Time:t}\n{closest.Uom}{closest.Value:0.00}\n{closest.Info}";

        var posCanvas = e.GetCurrentPoint(PART_Canvas).Position;
        Canvas.SetLeft(PART_Tooltip, posCanvas.X + 10);
        Canvas.SetTop(PART_Tooltip, posCanvas.Y - 10);
        PART_Tooltip.Visibility = Visibility.Visible;
    }

    void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        StopDotPulse();
        FadeOutTooltip();
        PART_HighlightDot.Visibility = Visibility.Collapsed;
    }

    double PlotX_Scoped(DateTime t)
    {
        long minX = _minX;
        long maxX = _maxX;
        return 40 + (ActualWidth - 50) * ((t.Ticks - minX) / (double)(maxX - minX));
    }

    double PlotY_Scoped(double v)
    {
        double maxY = _maxY;
        if (maxY <= 0)
            maxY = 1;

        return (ActualHeight - 30) - (ActualHeight - 40) * (v / maxY);
    }

    void StartDotPulse()
    {
        _dotPulseStoryboard?.Stop();

        var anim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.3,
            Duration = TimeSpan.FromSeconds(0.6),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        _dotPulseStoryboard = new Storyboard();
        Storyboard.SetTarget(anim, PART_HighlightDot);
        Storyboard.SetTargetProperty(anim, "Opacity");
        _dotPulseStoryboard.Children.Add(anim);
        _dotPulseStoryboard.Begin();
    }

    void StopDotPulse()
    {
        if (_dotPulseStoryboard != null)
        {
            _dotPulseStoryboard.Stop();
            _dotPulseStoryboard = null;
        }

        PART_HighlightDot.Opacity = 1.0;
    }

    void FadeInTooltip()
    {
        _tooltipFadeInStoryboard?.Stop();

        var anim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.6),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        _tooltipFadeInStoryboard = new Storyboard();
        Storyboard.SetTarget(anim, PART_Tooltip);
        Storyboard.SetTargetProperty(anim, "Opacity");
        _tooltipFadeInStoryboard.Children.Add(anim);
        _tooltipFadeInStoryboard.Begin();
    }

    void FadeOutTooltip()
    {
        if (_animating) { return; }
        _animating = true;

        // Create a NEW animation each time! (WinUI requirement)
        var anim = new DoubleAnimation
        {
            From = PART_Tooltip.Opacity,
            To = 0.0,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        anim.Completed += (_, _) =>
        {
            PART_Tooltip.Visibility = Visibility.Collapsed;
            _animating = false;
        };

        var sb = new Storyboard();
        Storyboard.SetTarget(anim, PART_Tooltip);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        sb.Begin();
    }

    [Obsolete("This method is kept for reference but should not be used. In WinUI3 a DoubleAnimation cannot be reused across multiple Storyboards, or multiple Begin() calls.", error: true)]
    void FadeOutTooltipDontUse()
    {
        if (_fadeOutTooltipAnimation == null || _animating) { return; }
        _animating = true;
        _tooltipFadeOutStoryboard?.Stop();
        _tooltipFadeOutStoryboard = new Storyboard();
        Storyboard.SetTarget(_fadeOutTooltipAnimation, PART_Tooltip);
        Storyboard.SetTargetProperty(_fadeOutTooltipAnimation, "Opacity");
        // In WinUI 3, a DoubleAnimation cannot be reused across multiple Storyboards, or multiple Begin() calls.
        _tooltipFadeOutStoryboard.Children.Add(_fadeOutTooltipAnimation); // System.Runtime.InteropServices.COMException (0x800F1000): No installed components were detected.
        _tooltipFadeOutStoryboard.Begin();
    }

    void ConfigureFadeOutAnimation()
    {
        if (_fadeOutTooltipAnimation != null)
            return;

        _fadeOutTooltipAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        _fadeOutTooltipAnimation.Completed += (_, _) =>
        {
            if (PART_Tooltip.Visibility == Visibility.Visible)
                PART_Tooltip.Visibility = Visibility.Collapsed;

            _animating = false;
        };
    }
    void Unload() => Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
}
