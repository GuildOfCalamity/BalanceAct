﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using BalanceAct.Services;
using BalanceAct.Support;
using BalanceAct.ViewModels;

using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using System.Text;

namespace BalanceAct;

public partial class App : Application
{
    #region [Props]
    int m_width = 1280;
    int m_height = 725;
    private Window? m_window;
    static UISettings m_UISettings = new UISettings();
    public new static App Current => (App)Application.Current; // Gets the current app instance (for Dependency Injection).
    public static string DatabaseExpense { get; private set; } = "ExpenseItems.json";
    public IServiceProvider Services { get; }
    public static IntPtr WindowHandle { get; set; }
    public static FrameworkElement? MainRoot { get; set; }
    public static AppWindow? AppWin { get; set; }
    public static Version WindowsVersion => Extensions.GetWindowsVersionUsingAnalyticsInfo();
    public static bool TransparencyEffectsEnabled { get => m_UISettings.AdvancedEffectsEnabled; }
    public static bool AnimationsEffectsEnabled { get => m_UISettings.AnimationsEnabled; }
    public static double TextScaleFactor { get => m_UISettings.TextScaleFactor; }
    public static bool IsClosing { get; set; } = false;
    public static bool IsWindowMaximized { get; set; }

    // https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/#advantages-and-disadvantages-of-packaging-your-app
#if IS_UNPACKAGED // We're using a custom PropertyGroup Condition we defined in the csproj to help us with the decision.
    public static bool IsPackaged { get => false; }
#else
    public static bool IsPackaged { get => true; }
#endif
    #endregion

    #region [Config]
    static bool _lastSave = false;
    static DateTime _lastMove = DateTime.Now;
    static Config? _localConfig;
    public static Config? LocalConfig
    {
        get => _localConfig;
        set => _localConfig = value;
    }

    public static Func<Config?, bool> SaveConfigFunc = (cfg) =>
    {
        if (cfg is not null)
        {
            cfg.firstRun = false;
            cfg.time = DateTime.Now;
            cfg.version = $"{GetCurrentAssemblyVersion()}";
            Process proc = Process.GetCurrentProcess();
            cfg.metrics = $"Process used {proc.PrivateMemorySize64 / 1024 / 1024}MB of memory and {proc.TotalProcessorTime.ToReadableString()} TotalProcessorTime on {Environment.ProcessorCount} possible cores.";
            try
            {
                _ = ConfigHelper.SaveConfig(cfg);
                return true;
            }
            catch (Exception) { return false; }
        }
        return false;
    };
    #endregion

    public App()
    {
        Debug.WriteLine($"[INFO] {System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        Debug.WriteLine($"[INFO] {ReflectPrimitive()}");

        App.Current.DebugSettings.FailFastOnErrors = false;
        
        #region [Exception handlers]
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomainFirstChanceException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        UnhandledException += ApplicationUnhandledException;
        #endregion

        Services = ConfigureServices();

        InitializeComponent();

        // https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.focusvisualkind?view=windows-app-sdk-1.3
        this.FocusVisualKind = FocusVisualKind.Reveal;

        if (Debugger.IsAttached)
        {
            this.DebugSettings.BindingFailed += DebugOnBindingFailed;
            this.DebugSettings.XamlResourceReferenceFailed += DebugOnXamlResourceReferenceFailed;
        }
    }

    static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        //services.AddSingleton<ILogger, FileLogger>();

        // Example of adding service with secondary constructor.
        if (App.IsPackaged)
            services.AddSingleton<ILogger, FileLogger>(sp => new FileLogger(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, LogLevel.Debug));
        else
            services.AddSingleton<ILogger, FileLogger>(sp => new FileLogger(System.AppContext.BaseDirectory, LogLevel.Debug));

        services.AddSingleton<IDataService, DataService>();

        // We could choose to pass the Window's MainRoot into the MainViewModel for invoking ContentDialogs.
        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }

    #region [Application Events]
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        #region [Load Config]
        if (ConfigHelper.DoesConfigExist())
        {
            try
            {
                LocalConfig = ConfigHelper.LoadConfig();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {nameof(ConfigHelper.LoadConfig)}: {ex.Message}");
            }
        }
        else // create default config if not found
        {
            try
            {
                LocalConfig = new Config
                {
                    firstRun = true,
                    version = $"{GetCurrentAssemblyVersion()}",
                    time = DateTime.Now,
                    metrics = "N/A",
                    windowW = m_width,
                    windowH = m_height,
                };
                ConfigHelper.SaveConfig(LocalConfig);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {nameof(ConfigHelper.SaveConfig)}: {ex.Message}");
            }
        }
        #endregion

        var appInst = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
        if (appInst != null)
        {
            Debug.WriteLine($"[INFO] Application ProcessId: {appInst.ProcessId}");
            var appActArgs = appInst.GetActivatedEventArgs();
            Debug.WriteLine($"[INFO] Activation args type: {appActArgs.Data.GetType().Name}");
            if (appActArgs.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs actEA)
            {
                Debug.WriteLine($"[INFO] Activation kind: {actEA.Kind}");
            }
        }

        // Instantiate our window object.
        m_window = new MainWindow();

        AppWin = GetAppWindow(m_window);
        if (AppWin != null)
        {
            // Gets or sets a value that indicates whether this window will appear in various system representations, such as ALT+TAB and taskbar.
            AppWin.IsShownInSwitchers = true;

            // We don't have the Closing event exposed by default, so we'll use the AppWindow to compensate.
            AppWin.Closing += (s, e) =>
            {
                App.IsClosing = true;
                Debug.WriteLine($"[INFO] Application closing detected at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
                _lastSave = SaveConfigFunc(LocalConfig);
            };

            // Destroying is always called, but Closing is only called when the application is shutdown normally.
            AppWin.Destroying += (s, e) =>
            {
                Debug.WriteLine($"[INFO] Application destroying detected at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
                if (!_lastSave) // prevent redundant calls
                    SaveConfigFunc(LocalConfig);
            };

            // The changed event contains the valuables, such as: position, size, visibility, z-order and presenter.
            AppWin.Changed += (s, args) =>
            {
                if (args.DidSizeChange)
                {
                    Debug.WriteLine($"[INFO] Window size changed to {s.Size.Width},{s.Size.Height}");
                    if (s.Presenter is not null && s.Presenter is OverlappedPresenter op)
                        IsWindowMaximized = op.State is OverlappedPresenterState.Maximized;

                    if (!IsWindowMaximized && LocalConfig is not null)
                    {
                        // Update width and height for profile settings.
                        LocalConfig!.windowH = s.Size.Height;
                        LocalConfig!.windowW = s.Size.Width;
                    }
                }

                if (args.DidPositionChange)
                {
                    if (s.Position.X > 0 && s.Position.Y > 0)
                    {
                        // This property is initially null. Once a window has been shown it always has a
                        // presenter applied, either one applied by the platform or applied by the app itself.
                        if (s.Presenter is not null && s.Presenter is OverlappedPresenter op)
                        {
                            if (op.State == OverlappedPresenterState.Minimized)
                            {
                                Debug.WriteLine($"[INFO] Window minimized");
                            }
                            else if (op.State != OverlappedPresenterState.Maximized && LocalConfig is not null)
                            {
                                Debug.WriteLine($"[INFO] Updating window position to {s.Position.X},{s.Position.Y} and size to {s.Size.Width},{s.Size.Height}");
                                // Update X and Y for profile settings.
                                LocalConfig!.windowX = s.Position.X;
                                LocalConfig!.windowY = s.Position.Y;
                            }
                            else
                            {
                                Debug.WriteLine($"[INFO] Ignoring position saving (window maximized or restored)");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[INFO] Ignoring zero/negative positional values");
                    }
                }
            };

            // Set the application icon.
            if (IsPackaged)
                AppWin.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Assets/StoreLogo.ico"));
            else
                AppWin.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, $"Assets/StoreLogo.ico"));

            AppWin.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        }

        m_window.Activate();
        
        // Save the FrameworkElement for any future content dialogs.
        MainRoot = m_window.Content as FrameworkElement;

        #region [Restore or center MainWindow]
        if (LocalConfig is not null)
        {
            if (LocalConfig.firstRun)
            {
                AppWin?.Resize(new Windows.Graphics.SizeInt32(m_width, m_height));
                CenterWindow(m_window);
            }
            else
            {
                AppWin?.MoveAndResize(new Windows.Graphics.RectInt32(LocalConfig.windowX, LocalConfig.windowY, LocalConfig.windowW, LocalConfig.windowH), Microsoft.UI.Windowing.DisplayArea.Primary);
            }
        }
        else
        {
            AppWin?.Resize(new Windows.Graphics.SizeInt32(m_width, m_height));
            CenterWindow(m_window);
        }
        #endregion
    }
    #endregion

    #region [Debugger Events]
    void DebugOnXamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs args)
    {
        Debug.WriteLine($"[WARNING] XamlResourceReferenceFailed: {args.Message}");
    }

    void DebugOnBindingFailed(object sender, BindingFailedEventArgs args)
    {
        Debug.WriteLine($"[WARNING] BindingFailed: {args.Message}");
    }
    #endregion

    #region [Window Helpers]
    /// <summary>
    /// This code example demonstrates how to retrieve an AppWindow from a WinUI3 window.
    /// The AppWindow class is available for any top-level HWND in your app.
    /// AppWindow is available only to desktop apps (both packaged and unpackaged), it's not available to UWP apps.
    /// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview
    /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.create?view=windows-app-sdk-1.3
    /// </summary>
    public Microsoft.UI.Windowing.AppWindow? GetAppWindow(object window)
    {
        // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // For other classes to use (mostly P/Invoke).
        App.WindowHandle = hWnd;

        // Retrieve the WindowId that corresponds to hWnd.
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

        // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        return appWindow;
    }

    /// <summary>
    /// Centers a <see cref="Microsoft.UI.Xaml.Window"/> based on the <see cref="Microsoft.UI.Windowing.DisplayArea"/>.
    /// </summary>
    /// <remarks>This must be run on the UI thread.</remarks>
    public static void CenterWindow(Window window)
    {
        if (window == null) { return; }

        try
        {
            System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId) is Microsoft.UI.Windowing.AppWindow appWindow &&
                Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest) is Microsoft.UI.Windowing.DisplayArea displayArea)
            {
                Windows.Graphics.PointInt32 CenteredPosition = appWindow.Position;
                CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                appWindow.Move(CenteredPosition);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// The <see cref="Microsoft.UI.Windowing.DisplayArea"/> exposes properties such as:
    /// OuterBounds     (Rect32)
    /// WorkArea.Width  (int)
    /// WorkArea.Height (int)
    /// IsPrimary       (bool)
    /// DisplayId.Value (ulong)
    /// </summary>
    /// <param name="window"></param>
    /// <returns><see cref="DisplayArea"/></returns>
    public Microsoft.UI.Windowing.DisplayArea? GetDisplayArea(Window window)
    {
        try
        {
            System.IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var da = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            return da;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {MethodBase.GetCurrentMethod()?.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// If <see cref="App.WindowHandle"/> is set then a call to User32 <see cref="SetForegroundWindow(nint)"/> 
    /// will be invoked. I tried using the native OverlappedPresenter.Restore(true), but that does not work.
    /// </summary>
    public static void ActivateMainWindow()
    {
        if (App.WindowHandle != IntPtr.Zero)
        {
            //_ = SetForegroundWindow(App.WindowHandle);
        }
        if (AppWin is not null && AppWin.Presenter is not null && AppWin.Presenter is OverlappedPresenter op)
        {
            op.Restore(true);
        }
    }
    #endregion

    #region [Domain Events]
    void ApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        Exception? ex = e.Exception;
        Debug.WriteLine($"[UnhandledException]: {ex?.Message}");
        Debug.WriteLine($"Unhandled exception of type {ex?.GetType()}: {ex}");
        DebugLog($"Unhandled Exception StackTrace: {Environment.StackTrace}");
        DebugLog($"{ex?.DumpFrames()}");
        e.Handled = true;
    }

    void CurrentDomainOnProcessExit(object? sender, EventArgs e)
    {
        if (!IsClosing)
            IsClosing = true;

        if (sender is null)
            return;

        if (sender is AppDomain ad)
        {
            Debug.WriteLine($"[OnProcessExit]", $"{nameof(App)}");
            Debug.WriteLine($"DomainID: {ad.Id}", $"{nameof(App)}");
            Debug.WriteLine($"FriendlyName: {ad.FriendlyName}", $"{nameof(App)}");
            Debug.WriteLine($"BaseDirectory: {ad.BaseDirectory}", $"{nameof(App)}");
        }
    }

    void CurrentDomainFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        Debug.WriteLine($"[ERROR] First chance exception from {sender?.GetType()}: {e.Exception.Message}");
        DebugLog($"First chance exception from {sender?.GetType()}: {e.Exception.Message}");
        if (e.Exception.InnerException != null)
            DebugLog($"  ⇨ InnerException: {e.Exception.InnerException.Message}");
        DebugLog($"First chance exception StackTrace: {Environment.StackTrace}");
        DebugLog($"{e.Exception.DumpFrames()}");
    }

    void CurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        Exception? ex = e.ExceptionObject as Exception;
        Debug.WriteLine($"[ERROR] Thread exception of type {ex?.GetType()}: {ex}");
        DebugLog($"Thread exception of type {ex?.GetType()}: {ex}");
        DebugLog($"{ex?.DumpFrames()}");
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception is AggregateException aex)
        {
            aex?.Flatten().Handle(ex =>
            {
                Debug.WriteLine($"[ERROR] Unobserved task exception: {ex?.Message}");
                DebugLog($"Unobserved task exception: {ex?.Message}");
                DebugLog($"{ex?.DumpFrames()}");
                return true;
            });
        }
        e.SetObserved(); // suppress and handle manually
    }
    #endregion

    #region [Reflection Helpers]
    /// <summary>
    /// Returns the declaring type's namespace.
    /// </summary>
    public static string? GetCurrentNamespace() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace;

    /// <summary>
    /// Returns the declaring type's full name.
    /// </summary>
    public static string? GetCurrentFullName() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly.FullName;

    /// <summary>
    /// Returns the declaring type's assembly name.
    /// </summary>
    public static string? GetCurrentAssemblyName() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

    /// <summary>
    /// Returns the AssemblyVersion, not the FileVersion.
    /// </summary>
    public static Version GetCurrentAssemblyVersion() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
    #endregion

    #region [Dialog Helpers]
    static SemaphoreSlim semaSlim = new SemaphoreSlim(1, 1);
    /// <summary>
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> does not look as nice as the
    /// <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> and is not part of the native Microsoft.UI.Xaml.Controls.
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> offers the <see cref="Windows.UI.Popups.UICommandInvokedHandler"/> 
    /// callback, but this could be replaced with actions. Both can be shown asynchronously.
    /// </summary>
    /// <remarks>
    /// You'll need to call <see cref="WinRT.Interop.InitializeWithWindow.Initialize"/> when using the <see cref="Windows.UI.Popups.MessageDialog"/>,
    /// because the <see cref="Microsoft.UI.Xaml.XamlRoot"/> does not exist and an owner must be defined.
    /// </remarks>
    public static async Task ShowMessageBox(string title, string message, string yesText, string noText, Action? yesAction, Action? noAction)
    {
        if (App.WindowHandle == IntPtr.Zero) { return; }

        // Create the dialog.
        var messageDialog = new MessageDialog($"{message}");
        messageDialog.Title = title;

        if (!string.IsNullOrEmpty(yesText))
        {
            messageDialog.Commands.Add(new UICommand($"{yesText}", (opt) => { yesAction?.Invoke(); }));
            messageDialog.DefaultCommandIndex = 0;
        }

        if (!string.IsNullOrEmpty(noText))
        {
            messageDialog.Commands.Add(new UICommand($"{noText}", (opt) => { noAction?.Invoke(); }));
            messageDialog.DefaultCommandIndex = 1;
        }

        // We must initialize the dialog with an owner.
        WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, App.WindowHandle);
        // Show the message dialog. Our DialogDismissedHandler will deal with what selection the user wants.
        await messageDialog.ShowAsync();
        // We could force the result in a separate timer...
        //DialogDismissedHandler(new UICommand("time-out"));
    }

    /// <summary>
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> does not look as nice as the
    /// <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> and is not part of the native Microsoft.UI.Xaml.Controls.
    /// The <see cref="Windows.UI.Popups.MessageDialog"/> offers the <see cref="Windows.UI.Popups.UICommandInvokedHandler"/> 
    /// callback, but this could be replaced with actions. Both can be shown asynchronously.
    /// </summary>
    /// <remarks>
    /// You'll need to call <see cref="WinRT.Interop.InitializeWithWindow.Initialize"/> when using the <see cref="Windows.UI.Popups.MessageDialog"/>,
    /// because the <see cref="Microsoft.UI.Xaml.XamlRoot"/> does not exist and an owner must be defined.
    /// </remarks>
    public static async Task ShowMessageBox(string title, string message, string primaryText, string cancelText)
    {
        // Create the dialog.
        var messageDialog = new MessageDialog($"{message}");
        messageDialog.Title = title;

        if (!string.IsNullOrEmpty(primaryText))
        {
            messageDialog.Commands.Add(new UICommand($"{primaryText}", new UICommandInvokedHandler(DialogDismissedHandler)));
            messageDialog.DefaultCommandIndex = 0;
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            messageDialog.Commands.Add(new UICommand($"{cancelText}", new UICommandInvokedHandler(DialogDismissedHandler)));
            messageDialog.DefaultCommandIndex = 1;
        }
        // We must initialize the dialog with an owner.
        WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, App.WindowHandle);
        // Show the message dialog. Our DialogDismissedHandler will deal with what selection the user wants.
        await messageDialog.ShowAsync();

        // We could force the result in a separate timer...
        //DialogDismissedHandler(new UICommand("time-out"));
    }

    /// <summary>
    /// Callback for the selected option from the user.
    /// </summary>
    static void DialogDismissedHandler(IUICommand command)
    {
        Debug.WriteLine($"[INFO] UICommand.Label ⇨ {command.Label}");
    }

    /// <summary>
    /// The <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> looks much better than the
    /// <see cref="Windows.UI.Popups.MessageDialog"/> and is part of the native Microsoft.UI.Xaml.Controls.
    /// The <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/> does not offer a <see cref="Windows.UI.Popups.UICommandInvokedHandler"/>
    /// callback, but in this example was replaced with actions. Both can be shown asynchronously.
    /// </summary>
    /// <remarks>
    /// There is no need to call <see cref="WinRT.Interop.InitializeWithWindow.Initialize"/> when using the <see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/>,
    /// but a <see cref="Microsoft.UI.Xaml.XamlRoot"/> must be defined since it inherits from <see cref="Microsoft.UI.Xaml.Controls.Control"/>.
    /// The <see cref="SemaphoreSlim"/> was added to prevent "COMException: Only one ContentDialog can be opened at a time."
    /// </remarks>
    public static async Task ShowDialogBox(string title, string message, string primaryText, string cancelText, Action? onPrimary, Action? onCancel, Uri? imageUri)
    {
        if (App.MainRoot?.XamlRoot == null) { return; }

        await semaSlim.WaitAsync();

        #region [Initialize Assets]
        double fontSize = 16;
        Microsoft.UI.Xaml.Media.FontFamily fontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");

        if (App.Current.Resources.TryGetValue("FontSizeMedium", out object _))
            fontSize = (double)App.Current.Resources["FontSizeMedium"];

        if (App.Current.Resources.TryGetValue("PrimaryFont", out object _))
            fontFamily = (Microsoft.UI.Xaml.Media.FontFamily)App.Current.Resources["PrimaryFont"];

        StackPanel panel = new StackPanel()
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical,
            Spacing = 10d
        };

        if (imageUri is not null)
        {
            panel.Children.Add(new Image
            {
                Margin = new Thickness(1, -50, 1, 1), // Move the image into the title area.
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
                Width = 40,
                Height = 40,
                Source = new BitmapImage(imageUri)
            });
        }

        panel.Children.Add(new TextBlock()
        {
            Text = message,
            FontSize = fontSize,
            FontFamily = fontFamily,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
        });

        var tb = new TextBox()
        {
            Text = message,
            FontSize = fontSize,
            FontFamily = fontFamily,
            TextWrapping = TextWrapping.Wrap
        };
        tb.Loaded += (s, e) => { tb.SelectAll(); };
        #endregion

        // NOTE: Content dialogs will automatically darken the background.
        ContentDialog contentDialog = new ContentDialog()
        {
            Title = title,
            PrimaryButtonText = primaryText,
            CloseButtonText = cancelText,
            Content = panel,
            XamlRoot = App.MainRoot?.XamlRoot,
            RequestedTheme = App.MainRoot?.ActualTheme ?? ElementTheme.Default
        };

        try
        {
            ContentDialogResult result = await contentDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    onPrimary?.Invoke();
                    break;
                //case ContentDialogResult.Secondary:
                //    onSecondary?.Invoke();
                //    break;
                case ContentDialogResult.None: // Cancel
                    onCancel?.Invoke();
                    break;
                default:
                    Debug.WriteLine($"Dialog result not defined.");
                    break;
            }
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Debug.WriteLine($"[ERROR] ShowDialogBox: {ex.Message}");
        }
        finally
        {
            semaSlim.Release();
        }
    }
    #endregion

    public static void CloseAllDialogs()
    {
        if (App.MainRoot?.XamlRoot == null) { return; }

        var openedDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(App.MainRoot?.XamlRoot);
        foreach (var item in openedDialogs)
        {
            if (item.Child is ContentDialog dialog)
                dialog.Hide();
        }
    }

    /// <summary>
    /// Simplified debug logger for app-wide use.
    /// </summary>
    /// <param name="message">the text to append to the file</param>
    public static void DebugLog(string message)
    {
        try
        {
            if (App.IsPackaged)
                System.IO.File.AppendAllText(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Debug.log"), $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}{Environment.NewLine}");
            else
                System.IO.File.AppendAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Debug.log"), $"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}{Environment.NewLine}");
        }
        catch (Exception)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt")}] {message}");
        }
    }

    /// <summary>
    /// Fetching framework version via primitive.
    /// </summary>
    public string ReflectPrimitive()
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            Assembly info = typeof(int).Assembly;
            var sig = info.Location;
            string[] separators = { "\\", "/" };
            var list = sig.Split(separators, StringSplitOptions.RemoveEmptyEntries).Skip(4);
            foreach (var str in list) { sb.Append($"{str} • "); }
        }
        catch (Exception) { }
        return $"{sb}";
    }
}
