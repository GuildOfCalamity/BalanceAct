using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using Windows.UI.Core;

namespace BalanceAct.Controls;

/*
<!-- [Example Styler] -->
<Style TargetType="local:ActiveImage">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="local:ActiveImage">
                <Grid>
                    <Image
                        x:Name="PART_Image"
                        MinWidth="10"
                        MinHeight="10"
                        Opacity="0"
                        RenderTransformOrigin="0.5,0.5">
                        <Image.RenderTransform>
                            <CompositeTransform />
                        </Image.RenderTransform>
                    </Image>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
*/

public enum ActiveImageMode
{
    Fade,                   // Default opacity fade (mandatory for other styles)
    SlideRight,             // Moves in from left
    SlideLeft,              // Moves in from right
    Zoom,                   // Scales up from small size
    ZoomAndRotate,          // Scales up and rotates
    RotateClockwise,        // 0-360 degree rotation
    RotateCounterClockwise  // 360-0 degree rotation
}

[TemplatePart(Name = "PART_Image", Type = typeof(Microsoft.UI.Xaml.Controls.Image))]
public sealed partial class ActiveImage : Control
{
    bool _initialized = false;
    bool _switching = false;
    Storyboard? _fadeInStoryboard;
    Storyboard? _fadeOutStoryboard;
    Image? _image;

    /// <summary>
    ///   Be sure to include a styler in your App.xaml
    /// </summary>
    public ActiveImage()
    {
        this.DefaultStyleKey = typeof(ActiveImage);
        this.Tapped += ActiveImageOnTapped;
    }

    void ActiveImageOnTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            var element = e.OriginalSource as FrameworkElement;
            var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            if (element != null && element.DataContext != null)
            {
                Debug.WriteLine($"[INFO] DataContext is of type '{element.DataContext.GetType()}'");
            }
            else if (element != null && element is Microsoft.UI.Xaml.Controls.Image img)
            {
                Debug.WriteLine($"[INFO] BaseUri of image is '{img?.BaseUri}'");
            }
            TapEvent?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ActiveImageOnTapped: {ex.Message}");
        }
    }

    /// <summary>
    /// NOTE: Initially OnApplyTemplate will not be called if <see cref="Visibility"/> 
    ///       of the control starts as <see cref="Visibility.Collapsed"/>.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _image = GetTemplateChild("PART_Image") as Image ?? throw new InvalidOperationException($"{nameof(ActiveImage)}: Style template missing 'PART_Image', cannot continue.");
        Debug.WriteLine($"[DEBUG] Got template child 'PART_Image'");
        InitializeAnimations();
        this.Visibility = Visibility.Collapsed;
        this.RegisterPropertyChangedCallback(VisibilityProperty, (s, d) => ((ActiveImage)s).OnVisibilityChanged());
        _initialized = true;
    }

    /// <summary>
    ///   This is the property that triggers the animations for the <see cref="ActiveImage"/> control.
    /// </summary>
    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
        nameof(IsVisible),
        typeof(bool),
        typeof(ActiveImage),
        new PropertyMetadata(false, OnIsVisiblePropertyChanged));

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }
    static void OnIsVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ActiveImage)d;
        if (e.NewValue is bool newValue)
            control.IsVisibleChanged(newValue);
    }
    void IsVisibleChanged(bool visible)
    {
        if (!_initialized)
        {
            // When the IsVisible property is set during startup we'll need to give
            // the control a little while to initialize, then set the property which
            // will be picked up by our RegisterPropertyChangedCallback().
            Task.Run(async () => { await Task.Delay(900); }).ContinueWith(t =>
            {
                if (visible)
                    this.DispatcherQueue.TryEnqueue(() => { this.Visibility = Visibility.Visible; });
                else
                    this.DispatcherQueue.TryEnqueue(() => { this.Visibility = Visibility.Collapsed; });
            });
            return;
        }

        if (visible)
            this.Visibility = Visibility.Visible;
        else
            this.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    ///   Dependency Property for ImageSource
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), 
        typeof(ImageSource),
        typeof(ActiveImage), 
        new PropertyMetadata(null, OnSourcePropertyChanged));

    public ImageSource Source
    {
        get => (ImageSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ActiveImage)d;
        if (e.NewValue is BitmapImage img)
            control.SetNewSource(img);
    }
    void SetNewSource(BitmapImage newImage)
    {
        if (_image != null)
        {
            _image.Source = newImage;
            if (IsVisible) // Run animation if source changed.
                _fadeInStoryboard?.Begin();
        }
        else
            Debug.WriteLine($"[WARNING] The '{nameof(_image)}' is null, cannot set source yet.");
    }

    /// <summary>
    ///   Dependency Property for Fade Duration
    /// </summary>
    public static readonly DependencyProperty FadeDurationProperty = DependencyProperty.Register(
        nameof(FadeDuration), 
        typeof(TimeSpan),
        typeof(ActiveImage), 
        new PropertyMetadata(TimeSpan.FromMilliseconds(300), OnFadeDurationChanged));

    public TimeSpan FadeDuration
    {
        get => (TimeSpan)GetValue(FadeDurationProperty);
        set => SetValue(FadeDurationProperty, value);
    }
    static void OnFadeDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ActiveImage)d;
        control.InitializeAnimations();
    }

    /// <summary>
    ///   Dependency Property for <see cref="ActiveImageMode"/>
    /// </summary>
    public static readonly DependencyProperty ImageModeProperty = DependencyProperty.Register(
        nameof(ImageMode), 
        typeof(ActiveImageMode),
        typeof(ActiveImage), 
        new PropertyMetadata(ActiveImageMode.Fade, OnImageModeChanged));

    public ActiveImageMode ImageMode
    {
        get => (ActiveImageMode)GetValue(ImageModeProperty);
        set => SetValue(ImageModeProperty, value);
    }
    static void OnImageModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ActiveImage)d;
        control.InitializeAnimations();
    }

    /// <summary>
    ///   Dependency Property for the TapEvent
    /// </summary>
    public static readonly DependencyProperty TapEventProperty = DependencyProperty.Register(
        nameof(TapEvent),
        typeof(Action),
        typeof(ActiveImage),
        new PropertyMetadata(null, OnTapEventPropertyChanged));
    public Action? TapEvent
    {
        get => (Action?)GetValue(TapEventProperty);
        set => SetValue(TapEventProperty, value);
    }
    /// <summary>
    /// Upon trigger, the <see cref="DependencyObject"/> will be the control itself (<see cref="ProgressButton"/>)
    /// and the <see cref="DependencyPropertyChangedEventArgs"/> will be the <see cref="Action"/> object contained within.
    /// </summary>
    static void OnTapEventPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null && e.NewValue is Action act)
        {
            Debug.WriteLine($"[OnTapEventPropertyChanged] => {act}");
        }
        else if (e.NewValue != null)
        {
            Debug.WriteLine($"[WARNING] Wrong type => {e.NewValue?.GetType()}");
            Debugger.Break();
        }
    }

    void InitializeAnimations()
    {
        if (_image is null)
            return;

        if (Source is not null)
        {
            _image.Source = Source;
            _image.Width = Width;
            _image.Height = Height;
        }

        _fadeInStoryboard = new Storyboard();
        _fadeOutStoryboard = new Storyboard();

        #region [Opacity effect mandatory]
        var fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(FadeDuration),
            EnableDependentAnimation = true,
            EasingFunction = new QuadraticEase()
        };
        Storyboard.SetTarget(fadeInAnimation, _image);
        Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
        _fadeInStoryboard.Children.Add(fadeInAnimation);

        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(FadeDuration),
            EnableDependentAnimation = true,
            EasingFunction = new QuadraticEase()
        };
        Storyboard.SetTarget(fadeOutAnimation, _image);
        Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
        _fadeOutStoryboard.Children.Add(fadeOutAnimation);
        _fadeOutStoryboard.Completed -= FadeOutStoryboardOnCompleted;
        _fadeOutStoryboard.Completed += FadeOutStoryboardOnCompleted;
        #endregion
        #region [Slide Right Effect]
        if (ImageMode == ActiveImageMode.SlideRight)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            double amount = -100;
            if (Width != double.NaN && Width > 0)
                amount = Width * -1;
            var slideAnimation = new DoubleAnimation
            {
                From = amount, // alway slide from left to right
                To = 0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(slideAnimation, _image);
            Storyboard.SetTargetProperty(slideAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            _fadeInStoryboard.Children.Add(slideAnimation);
        }
        #endregion
        #region [Slide Left Effect]
        else if (ImageMode == ActiveImageMode.SlideLeft)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            double amount = 100;
            if (Width != double.NaN && Width > 0)
                amount = Width;
            var slideAnimation = new DoubleAnimation
            {
                From = amount, // alway slide from left to right
                To = 0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(slideAnimation, _image);
            Storyboard.SetTargetProperty(slideAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            _fadeInStoryboard.Children.Add(slideAnimation);
        }
        #endregion
        #region [Zoom Effect]
        else if (ImageMode == ActiveImageMode.Zoom)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            var zoomXAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 1.0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(zoomXAnimation, _image);
            Storyboard.SetTargetProperty(zoomXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            _fadeInStoryboard.Children.Add(zoomXAnimation);

            var zoomYAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 1.0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(zoomYAnimation, _image);
            Storyboard.SetTargetProperty(zoomYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            _fadeInStoryboard.Children.Add(zoomYAnimation);
        }
        #endregion
        #region [Zoom & Rotate Effect]
        else if (ImageMode == ActiveImageMode.ZoomAndRotate)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            var zoomXAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 1.0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(zoomXAnimation, _image);
            Storyboard.SetTargetProperty(zoomXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            _fadeInStoryboard.Children.Add(zoomXAnimation);

            var zoomYAnimation = new DoubleAnimation
            {
                From = 0.2,
                To = 1.0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(zoomYAnimation, _image);
            Storyboard.SetTargetProperty(zoomYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            _fadeInStoryboard.Children.Add(zoomYAnimation);

            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(rotateAnimation, _image);
            Storyboard.SetTargetProperty(rotateAnimation, "(UIElement.RenderTransform).(CompositeTransform.Rotation)");
            _fadeInStoryboard.Children.Add(rotateAnimation);
        }
        #endregion
        #region [Rotate Clockwise Effect]
        else if (ImageMode == ActiveImageMode.RotateClockwise)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(rotateAnimation, _image);
            Storyboard.SetTargetProperty(rotateAnimation, "(UIElement.RenderTransform).(CompositeTransform.Rotation)");
            _fadeInStoryboard.Children.Add(rotateAnimation);
        }
        #endregion
        #region [Rotate Counter Clockwise Effect]
        else if (ImageMode == ActiveImageMode.RotateCounterClockwise)
        {
            /* NOTE: The styler must contain a CompositeTransform, e.g.
            <Image>
               <Image.RenderTransform>
                  <CompositeTransform />
               </Image.RenderTransform>
            </Image>
            */
            var rotateAnimation = new DoubleAnimation
            {
                From = 360,
                To = 0,
                Duration = new Duration(FadeDuration),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(rotateAnimation, _image);
            Storyboard.SetTargetProperty(rotateAnimation, "(UIElement.RenderTransform).(CompositeTransform.Rotation)");
            _fadeInStoryboard.Children.Add(rotateAnimation);
        }
        #endregion
    }

    void OnVisibilityChanged()
    {
        if (!_initialized || _switching)
            return;

        if (this.Visibility == Visibility.Visible)
        {
            _image.Opacity = 0;
            _fadeInStoryboard?.Begin();
        }
        else if (this.Visibility == Visibility.Collapsed)
        {
            _switching = true;
            this.Visibility = Visibility.Visible;
            _fadeOutStoryboard?.Begin();
        }
    }

    void FadeOutStoryboardOnCompleted(object? sender, object e)
    {
        Debug.WriteLine($"[INFO] FadeOutStoryboard was completed.");
        this.Visibility = Visibility.Collapsed;
        _switching = false; // This line must be kept after the visibility change.
    }

}
