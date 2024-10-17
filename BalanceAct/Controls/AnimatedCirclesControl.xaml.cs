using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace BalanceAct.Controls;

/// <summary>
/// <example><code>
///   animatedCirclesControl.CircleSize = 15;
///   animatedCirclesControl.ScaleFactor = 1.4;
///   animatedCirclesControl.AnimationFrequency = 0.1; // Produce undulate effect
///   animatedCirclesControl.AnimationFrequency = 0.4; // Produce alternate effect
///   animatedCirclesControl.GradientColorOne = Microsoft.UI.Colors.WhiteSmoke;
///   animatedCirclesControl.GradientColorTwo = Microsoft.UI.Colors.DodgerBlue;
///   animatedCirclesControl.IsRunning = true;  // Start the animation
///   animatedCirclesControl.IsRunning = false; // Stop the animation
/// </code></example>
/// </summary>
public sealed partial class AnimatedCirclesControl : UserControl
{
    static bool loaded = false;
    Storyboard? storyboard1;
    Storyboard? storyboard2;
    Storyboard? storyboard3;

    #region [Dependency Properties]
    /// <summary>
    /// DependencyProperty for CircleSize
    /// </summary>
    public double CircleSize
    {
        get => (double)GetValue(CircleSizeProperty);
        set => SetValue(CircleSizeProperty, value);
    }
    public static readonly DependencyProperty CircleSizeProperty = DependencyProperty.Register(
        nameof(CircleSize),
        typeof(double),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(15.0));

    /// <summary>
    /// DependencyProperty for AnimationDuration
    /// </summary>
    public double AnimationDuration
    {
        get => (double)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }
    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
        nameof(AnimationDuration),
        typeof(double),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(0.8));

    /// <summary>
    /// DependencyProperty for AnimationFrequency
    /// </summary>
    public double AnimationFrequency
    {
        get => (double)GetValue(AnimationFrequencyProperty);
        set => SetValue(AnimationFrequencyProperty, value);
    }
    public static readonly DependencyProperty AnimationFrequencyProperty = DependencyProperty.Register(
        nameof(AnimationFrequency),
        typeof(double),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(0.1));

    /// <summary>
    /// DependencyProperty for outer gradient color (1st color in gradient)
    /// </summary>
    public Color GradientColorOne
    {
        get => (Color)GetValue(GradientColorOneProperty);
        set => SetValue(GradientColorOneProperty, value);
    }
    public static readonly DependencyProperty GradientColorOneProperty = DependencyProperty.Register(
        nameof(GradientColorOne),
        typeof(Color),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(Windows.UI.Color.FromArgb(255, 48, 0, 255), OnGradientColorChanged));

    /// <summary>
    /// DependencyProperty for inner gradient color (2nd color in gradient)
    /// </summary>
    public Color GradientColorTwo
    {
        get => (Color)GetValue(GradientColorTwoProperty);
        set => SetValue(GradientColorTwoProperty, value);
    }
    public static readonly DependencyProperty GradientColorTwoProperty = DependencyProperty.Register(
        nameof(GradientColorTwo),
        typeof(Color),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(Windows.UI.Color.FromArgb(255, 208, 208, 255), OnGradientColorChanged));

    /// <summary>
    /// DependencyProperty for ScaleFactor
    /// </summary>
    public double ScaleFactor
    {
        get => (double)GetValue(ScaleFactorProperty);
        set => SetValue(ScaleFactorProperty, value);
    }
    public static readonly DependencyProperty ScaleFactorProperty = DependencyProperty.Register(
        nameof(ScaleFactor),
        typeof(double),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(1.4, OnScaleFactorChanged));

    /// <summary>
    /// DependencyProperty for IsRunning
    /// </summary>
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(false, OnIsRunningChanged));

    /// <summary>
    /// <para>
    ///   DependencyProperty for UseRadialGradient.
    /// </para>
    /// <para>
    ///   If false then a linear gradient will be 
    ///   used. If you wish to have no gradient effect, 
    ///   then set both colors to the same value.
    /// </para>
    /// </summary>
    public bool UseRadialGradient
    {
        get => (bool)GetValue(UseRadialGradientProperty);
        set => SetValue(UseRadialGradientProperty, value);
    }
    public static readonly DependencyProperty UseRadialGradientProperty = DependencyProperty.Register(
        nameof(UseRadialGradient),
        typeof(bool),
        typeof(AnimatedCirclesControl),
        new PropertyMetadata(false, OnUseRadialGradientChanged));
    #endregion

    /// <summary>
    /// Default constructor
    /// </summary>
    public AnimatedCirclesControl()
    {
        this.InitializeComponent();
        this.Loaded += AnimatedCirclesControl_Loaded;
    }

    /// <summary>
    /// Setting the ellipse fill in the XAML does not work, so we will update
    /// the gradient brush in the code-behind when the control is loaded.
    /// </summary>
    void AnimatedCirclesControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateRadialGradientBrush();
        double margin = 10;
        if (ScaleFactor >= 1.0)
            margin = (CircleSize * ScaleFactor) / 2.05d;
        else
            margin = (CircleSize + 1d) / 2.05d;
        Debug.WriteLine($"[INFO] Calculated margin is {margin}");
        Circle2.Margin = new Thickness(margin, 0, 0, 0);
        Circle3.Margin = new Thickness(margin, 0, 0, 0);

        loaded = true;
    }

    /// <summary>
    /// Begin circle animations
    /// </summary>
    void StartAnimation()
    {
        if (storyboard1 == null || storyboard2 == null || storyboard3 == null)
            ConfigureAnimations();

        storyboard1?.Begin();
        storyboard2?.Begin();
        storyboard3?.Begin();

        CirclesContainer.Visibility = Visibility.Visible;
        FadeInCircles();
    }

    /// <summary>
    /// Applies the <see cref="DoubleAnimation"/>s to our <see cref="Storyboard"/>s.
    /// </summary>
    void ConfigureAnimations()
    {
        storyboard1 = CreateGrowShrinkAnimation(Scale1, 0d, ScaleFactor);
        storyboard2 = CreateGrowShrinkAnimation(Scale2, AnimationFrequency, ScaleFactor);
        storyboard3 = CreateGrowShrinkAnimation(Scale3, AnimationFrequency * 2, ScaleFactor);
    }

    /// <summary>
    /// End circle animations
    /// </summary>
    void StopAnimation()
    {
        // In the event that the user changes the ScaleFactor
        // on initial startup, we want to skip the fade out
        // so that the animations can be reconfigured.
        if (loaded)
        {
            FadeOutCircles();
        }
        else
        {
            storyboard1?.Stop();
            storyboard2?.Stop();
            storyboard3?.Stop();
        }
    }

    /// <summary>
    /// Dependency callback for <see cref="IsRunning"/> property.
    /// </summary>
    static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnimatedCirclesControl)d;
        bool isRunning = (bool)e.NewValue;
        if (control != null && isRunning)
            control.StartAnimation();
        else if (control != null && !isRunning)
            control.StopAnimation();
    }

    /// <summary>
    /// Dependency callback for <see cref="ScaleFactor"/> property.
    /// </summary>
    static void OnScaleFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnimatedCirclesControl)d;
        if (control == null)
            return;

        if ((double)e.NewValue <= 1.0d)
            control.ScaleFactor = 1.1;
        else
            control.ScaleFactor = (double)e.NewValue;

        Debug.WriteLine($"[INFO] Loaded flag is {loaded}");

        // The ScaleFactor is used in the DoubleAnimation,
        // so we'll need to reconfigure them if changed.
        control.ConfigureAnimations();

        // In the event that the user changes the ScaleFactor
        // on initial startup, we want to restart the animations.
        if (!loaded && control.IsRunning)
        {
            control.StopAnimation();
            control.StartAnimation();
        }
    }

    /// <summary>
    /// Setup and configure the animations.
    /// </summary>
    Storyboard CreateGrowShrinkAnimation(ScaleTransform scaleTransform, double delay, double toSize = 1.4)
    {
        Storyboard storyboard = new Storyboard();

        // Easing function for smoother transitions
        EasingFunctionBase easing = new SineEase { EasingMode = EasingMode.EaseInOut };

        // Create grow animation for ScaleX
        DoubleAnimation growAnimationX = new DoubleAnimation
        {
            To = toSize, // Grow by x
            Duration = new Duration(TimeSpan.FromSeconds(AnimationDuration / 2)),
            AutoReverse = true, // Shrink back
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(delay), // Delay animation start
            EasingFunction = easing // Apply easing
        };

        // Apply to ScaleX
        Storyboard.SetTarget(growAnimationX, scaleTransform);
        Storyboard.SetTargetProperty(growAnimationX, "ScaleX");
        storyboard.Children.Add(growAnimationX);

        // Create grow animation for ScaleY
        DoubleAnimation growAnimationY = new DoubleAnimation
        {
            To = toSize, // Grow by x
            Duration = new Duration(TimeSpan.FromSeconds(AnimationDuration / 2)),
            AutoReverse = true, // Shrink back
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(delay), // Delay animation start
            EasingFunction = easing // Apply easing
        };

        // Apply to ScaleY
        Storyboard.SetTarget(growAnimationY, scaleTransform);
        Storyboard.SetTargetProperty(growAnimationY, "ScaleY");
        storyboard.Children.Add(growAnimationY);

        return storyboard;
    }

    /// <summary>
    /// Fade in the circles
    /// </summary>
    void FadeInCircles()
    {
        Storyboard fadeInStoryboard = new Storyboard();
        DoubleAnimation fadeIn = new DoubleAnimation
        {
            To = 1, // Fully visible
            Duration = new Duration(TimeSpan.FromSeconds(0.25))
        };
        Storyboard.SetTarget(fadeIn, CirclesContainer);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        fadeInStoryboard.Children.Add(fadeIn);
        fadeInStoryboard.Begin();
    }

    /// <summary>
    /// Fade out the circles and hide after completion
    /// </summary>
    void FadeOutCircles()
    {
        Storyboard fadeOutStoryboard = new Storyboard();
        DoubleAnimation fadeOut = new DoubleAnimation
        {
            To = 0, // Fully transparent
            Duration = new Duration(TimeSpan.FromSeconds(0.25))
        };
        fadeOut.Completed += (s, e) =>
        {
            CirclesContainer.Visibility = Visibility.Collapsed;
            storyboard1?.Stop();
            storyboard2?.Stop();
            storyboard3?.Stop();
        };
        Storyboard.SetTarget(fadeOut, CirclesContainer);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        fadeOutStoryboard.Children.Add(fadeOut);
        fadeOutStoryboard.Begin();
    }

    /// <summary>
    /// Setting the ellipse fill in the XAML does not work, so we will update
    /// the gradient brush in the code-behind when the color changes.
    /// </summary>
    static void OnGradientColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnimatedCirclesControl)d;
        control?.UpdateRadialGradientBrush();
    }

    /// <summary>
    /// Dependency callback for <see cref="UseRadialGradient"/> property.
    /// </summary>
    static void OnUseRadialGradientChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnimatedCirclesControl)d;
        control?.UpdateRadialGradientBrush();
    }

    /// <summary>
    /// Create and apply the radial gradient brush for all circles
    /// </summary>
    void UpdateRadialGradientBrush()
    {
        if (UseRadialGradient)
            Circle1.Fill = Circle2.Fill = Circle3.Fill = GradientColorTwo.CreateRadialGradientBrush(GradientColorOne);
        else
            Circle1.Fill = Circle2.Fill = Circle3.Fill = GradientColorTwo.CreateLinearGradientBrush(GradientColorOne);
    }
}

public static class AnimatedCirclesExtensions
{
    static bool injectAdditionalGradientStop = false;

    /// <summary>
    /// Helper method for creating <see cref="LinearGradientBrush"/>s.
    /// </summary>
    public static LinearGradientBrush CreateLinearGradientBrush(this Color c1, Color c2)
    {
        var lgb = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(0, 1)
        };
        lgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        if (injectAdditionalGradientStop && !AreIdentical(c1, c2))
        {
            //var cr = CalculateContrastRatio(c1, c2);
            //Debug.WriteLine($"[INFO] ContrastRatio is {cr:N2}");
            var dominant = ExtractPredominantColor(new Windows.UI.Color[] { c1, c2 });
            Debug.WriteLine($"[INFO] Predominant color is {dominant}");
            lgb.GradientStops.Add(new GradientStop { Color = dominant.LighterBy(0.2f), Offset = 0.36 });
        }
        lgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 0.57 });
        lgb.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;
        //lgb.SpreadMethod = GradientSpreadMethod.Pad;
        return lgb;
    }

    /// <summary>
    /// Helper method for creating <see cref="RadialGradientBrush"/>s.
    /// </summary>
    public static RadialGradientBrush CreateRadialGradientBrush(this Color c1, Color c2)
    {
        var rgb = new RadialGradientBrush();
        rgb.Center = new Windows.Foundation.Point(0.5, 0.5);
        rgb.RadiusX = 0.5; rgb.RadiusY = 0.5;
        rgb.FallbackColor = Colors.SpringGreen;
        rgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        rgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 1.0 });
        return rgb;
    }

    /// <summary>
    /// Helper method for creating <see cref="RadialGradientBrush"/>s.
    /// </summary>
    public static RadialGradientBrush CreateRadialGradientBrush(this Color c1, Color c2, Color c3)
    {
        var rgb = new RadialGradientBrush();
        rgb.Center = new Windows.Foundation.Point(0.5, 0.5);
        rgb.RadiusX = 0.5; rgb.RadiusY = 0.5;
        rgb.FallbackColor = Colors.SpringGreen;
        rgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        rgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 0.5 });
        rgb.GradientStops.Add(new GradientStop { Color = c3, Offset = 1.0 });
        return rgb;
    }

    /// <summary>
    /// Darkens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to darken. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static Windows.UI.Color DarkerBy(this Windows.UI.Color color, float amount)
    {
        return color.Lerp(Colors.Black, amount);
    }

    /// <summary>
    /// Lightens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to lighten. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static Windows.UI.Color LighterBy(this Windows.UI.Color color, float amount)
    {
        return color.Lerp(Colors.White, amount);
    }

    /// <summary>
    /// Finds the contrast ratio.
    /// This is helpful for determining if one control's foreground and another control's background will be hard to distinguish.
    /// https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    /// (L1 + 0.05) / (L2 + 0.05), where
    /// L1 is the relative luminance of the lighter of the colors, and
    /// L2 is the relative luminance of the darker of the colors.
    /// </summary>
    /// <param name="first"><see cref="Windows.UI.Color"/></param>
    /// <param name="second"><see cref="Windows.UI.Color"/></param>
    /// <returns>ratio between relative luminance</returns>
    public static double CalculateContrastRatio(Windows.UI.Color first, Windows.UI.Color second)
    {
        double relLuminanceOne = GetRelativeLuminance(first);
        double relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05) / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    /// <summary>
    /// Gets the relative luminance.
    /// https://www.w3.org/WAI/GL/wiki/Relative_luminance
    /// For the sRGB colorspace, the relative luminance of a color is defined as L = 0.2126 * R + 0.7152 * G + 0.0722 * B
    /// </summary>
    /// <param name="c"><see cref="Windows.UI.Color"/></param>
    /// <remarks>This is mainly used by <see cref="Helpers.CalculateContrastRatio(Color, Color)"/></remarks>
    public static double GetRelativeLuminance(Windows.UI.Color c)
    {
        double rSRGB = c.R / 255.0;
        double gSRGB = c.G / 255.0;
        double bSRGB = c.B / 255.0;

        // WebContentAccessibilityGuideline 2.x definition was 0.03928 (incorrect)
        // WebContentAccessibilityGuideline 3.x definition is 0.04045 (correct)
        double r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow(((rSRGB + 0.055) / 1.055), 2.4);
        double g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow(((gSRGB + 0.055) / 1.055), 2.4);
        double b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow(((bSRGB + 0.055) / 1.055), 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Determine if two colors are the same.
    public static bool AreIdentical(Windows.UI.Color c1, Windows.UI.Color c2)
    {
        return (c1.A == c2.A && c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);
    }

    /// <summary>
    /// Calculates the linear interpolated Color based on the given Color values.
    /// </summary>
    /// <param name="colorFrom">Source Color.</param>
    /// <param name="colorTo">Target Color.</param>
    /// <param name="amount">Weight given to the target color.</param>
    /// <returns>Linear Interpolated Color.</returns>
    public static Windows.UI.Color Lerp(this Windows.UI.Color colorFrom, Windows.UI.Color colorTo, float amount)
    {
        // Convert colorFrom components to lerp-able floats
        float sa = colorFrom.A, sr = colorFrom.R, sg = colorFrom.G, sb = colorFrom.B;

        // Convert colorTo components to lerp-able floats
        float ea = colorTo.A, er = colorTo.R, eg = colorTo.G, eb = colorTo.B;

        // lerp the colors to get the difference
        byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
             r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
             g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
             b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

        // return the new color
        return Windows.UI.Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// Helper method for extracting the dominant color from an array of <see cref="Windows.UI.Color"/>s.
    /// </summary>
    /// <returns><see cref="Windows.UI.Color"/></returns>
    public static Windows.UI.Color ExtractPredominantColor(Windows.UI.Color[] colors)
    {
        Dictionary<uint, int> dict = new Dictionary<uint, int>();
        uint maxColor = 0xff000000;

        // Take a small sampling of the decoded pixels, looking for the most common color
        int pixelSamples = Math.Min(2000, colors.Length);
        int skipPixels = colors.Length / pixelSamples;

        for (int pixel = colors.Length - 1; pixel >= 0; pixel -= skipPixels)
        {
            Windows.UI.Color c = colors[pixel];

            // Quantize the colors to bucket the groupings better
            c.R -= (byte)(c.R % 10);
            c.G -= (byte)(c.G % 10);
            c.B -= (byte)(c.B % 10);

            // Determine the saturation and value for the color
            int max = Math.Max(c.R, Math.Max(c.G, c.B));
            int min = Math.Min(c.R, Math.Min(c.G, c.B));
            int saturation = (int)(((max == 0) ? 0 : (1f - (1f * min / max))) * 255);
            int value = (int)((max / 255f) * 255);

            if (c.A > 0)
            {
                uint color = (uint)((255 << 24) | (c.R << 16) | (c.G << 8) | (c.B << 0));

                // Weigh the saturated/high-value colors more heavily
                int weight = saturation + value;

                if (dict.ContainsKey(color))
                    dict[color] += weight;
                else
                    dict.Add(color, weight);
            }
        }

        // Determine the predominant color
        int maxValue = 0;
        foreach (KeyValuePair<uint, int> pair in dict)
        {
            if (pair.Value > maxValue)
            {
                maxColor = pair.Key;
                maxValue = pair.Value;
            }
        }

        // Convert to the final color value
        return Windows.UI.Color.FromArgb((byte)(maxColor >> 24), (byte)(maxColor >> 16), (byte)(maxColor >> 8), (byte)(maxColor >> 0));
    }

    public static Windows.UI.Color GetRandomWinUIColor()
    {
        byte[] buffer = new byte[3];
        Random.Shared.NextBytes(buffer);
        return Windows.UI.Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);
    }


    /// <summary>
    /// Clamping function for any value of type <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="val">initial value</param>
    /// <param name="min">lowest range</param>
    /// <param name="max">highest range</param>
    /// <returns>clamped <paramref name="val"/></returns>
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
    }

    /// <summary>
    /// Multi-step clamping function.
    /// </summary>
    /// <param name="value">the value to test</param>
    /// <param name="max">the final limit</param>
    /// <returns>clamped <paramref name="value"/></returns>
    public static double MultiClamp(this double value, double max)
    {
        while (value >= max) { value = value.Clamp(value, value / 1.5f); }
        return value;
    }

    public static Windows.UI.Color[] CreateColorScale(int start, int end)
    {
        var colors = new Windows.UI.Color[end - start + 1];
        for (int i = 0; i < colors.Length; i++)
        {
            float factor = ((float)i / (end - start)) * 255; // map the position to 0-255
            // Create a color gradient from light to dark (ignore blue channel)
            colors[i] = Windows.UI.Color.FromArgb(255, (byte)(200 * factor), (byte)(255 - 10 * factor), 0);
        }
        return colors;
    }

    public static float ScaleValueVector(float begin, float end, int divy = 100)
    {
        var result = System.Numerics.Vector3.Dot(new System.Numerics.Vector3(begin, begin, 0), new System.Numerics.Vector3(end, end, 0));
        return result > 0 ? result / divy : result;
    }

    /// <summary>
    /// Linear interpolation for a range of floats.
    /// </summary>
    public static float Lerp(this float start, float end, float amount = 0.5F) => start + (end - start) * amount;
    /// <summary>
    /// Linear interpolation for a range of double.
    /// </summary>
    public static double Lerp(this double start, double end, double amount = 0.5F) => start + (end - start) * amount;
}
