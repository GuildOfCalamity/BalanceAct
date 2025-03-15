using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.Graphics.Imaging;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()

namespace BalanceAct;

public sealed partial class MainWindow : Window
{
    bool _useGradient = true;
    SystemBackdropConfiguration? _configurationSource;
    DesktopAcrylicController? _acrylicController;

    public MainWindow()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        InitializeComponent();

        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
            this.ExtendsContentIntoTitleBar = true;
            this.Title = $"{App.GetCurrentAssemblyName()}";
            SetTitleBar(CustomTitleBar);
        }

        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        this.Closed += MainWindow_Closed;

        if (_useGradient)
        {
            CreateGradientBackdrop(root, new System.Numerics.Vector2(0.9f, 1));
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            // https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-backdrop-controller
            // Hook up the policy object.
            _configurationSource = new SystemBackdropConfiguration();
            // Create the desktop controller.
            _acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();
            _acrylicController.TintOpacity = 0.4f; // Lower values may be too translucent vs light background.
            _acrylicController.LuminosityOpacity = 0.1f;
            _acrylicController.TintColor = Microsoft.UI.Colors.Gray;
            // Fall-back color is only used when the window state becomes deactivated.
            _acrylicController.FallbackColor = Microsoft.UI.Colors.Transparent;
            // Note: Be sure to have "using WinRT;" to support the Window.As<T>() call.
            _acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
        }
        else
        {
            if (App.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out object _))
                root.Background = (Microsoft.UI.Xaml.Media.SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"];
            else
                root.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));
        }

        _ = Task.Run(async () =>
        {
            if (dispatcher is null) { return; }
            try
            {
                SoftwareBitmap? sftbmp = await Support.BlurHelper.LoadSoftwareBitmapFromPathAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Balance2.png"));
                if (sftbmp != null)
                {
                    var source = Support.BlurHelper.ApplyBlur(sftbmp, 4);
                    dispatcher.TryEnqueue(async () => { imgWatermark.Source = await Support.BlurHelper.ConvertSoftwareBitmapToBitmapImageAsync(source); });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WARNING] During blur apply: {ex.Message}");
            }
        });
    }

    void CreateGradientBackdrop(FrameworkElement fe, System.Numerics.Vector2 endPoint)
    {
        // Get the FrameworkElement's compositor.
        var compositor = ElementCompositionPreview.GetElementVisual(fe).Compositor;
        if (compositor == null) { return; }
        var gb = compositor.CreateLinearGradientBrush();

        // Define gradient stops.
        var gradientStops = gb.ColorStops;

        // If we found our App.xaml brushes then use them.
        if (App.Current.Resources.TryGetValue("GC1", out object clr1) &&
            App.Current.Resources.TryGetValue("GC2", out object clr2) &&
            App.Current.Resources.TryGetValue("GC3", out object clr3) &&
            App.Current.Resources.TryGetValue("GC4", out object clr4))
        {
            //var clr1 = (Windows.UI.Color)App.Current.Resources["GC1"];
            //var clr2 = (Windows.UI.Color)App.Current.Resources["GC2"];
            //var clr3 = (Windows.UI.Color)App.Current.Resources["GC3"];
            //var clr4 = (Windows.UI.Color)App.Current.Resources["GC4"];
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, (Windows.UI.Color)clr1));
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, (Windows.UI.Color)clr2));
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, (Windows.UI.Color)clr3));
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, (Windows.UI.Color)clr4));
        }
        else
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, Windows.UI.Color.FromArgb(55, 255, 0, 0)));   // Red
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, Windows.UI.Color.FromArgb(55, 255, 216, 0))); // Yellow
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, Windows.UI.Color.FromArgb(55, 0, 255, 0)));   // Green
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, Windows.UI.Color.FromArgb(55, 0, 0, 255)));   // Blue
        }

        // Set the direction of the gradient.
        gb.StartPoint = new System.Numerics.Vector2(0, 0);
        //gb.EndPoint = new System.Numerics.Vector2(1, 1);
        gb.EndPoint = endPoint;

        // Create a sprite visual and assign the gradient brush.
        var spriteVisual = Compositor.CreateSpriteVisual();
        spriteVisual.Brush = gb;

        // Set the size of the sprite visual to cover the entire window.
        spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualSize.X, (float)fe.ActualSize.Y);

        // Handle the SizeChanged event to adjust the size of the sprite visual when the window is resized.
        fe.SizeChanged += (s, e) =>
        {
            spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualWidth, (float)fe.ActualHeight);
        };

        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(fe, spriteVisual);
    }

    void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // Make sure the Acrylic controller is disposed so it doesn't try to access a closed window.
        if (_acrylicController is not null)
        {
            _acrylicController.Dispose();
            _acrylicController = null;
        }
    }
}
