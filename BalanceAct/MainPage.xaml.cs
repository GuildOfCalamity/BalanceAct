﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using BalanceAct.Models;
using BalanceAct.Services;
using BalanceAct.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;

namespace BalanceAct;

public sealed partial class MainPage : Page
{
    bool useBloom = true;
    MainViewModel? ViewModel = App.Current.Services.GetService<MainViewModel>();
    FileLogger? Logger = (FileLogger?)App.Current.Services.GetService<ILogger>();

    public MainPage()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        InitializeComponent();
        this.Loading += MainPageLoading;
        ItemListView.RightTapped += ItemListView_RightTapped;
        Controls.PlotControl.ExpenseItemPlotTap += (ei) => SetSelectedItem(ei);
        //foreach (var ele in Extensions.GetHierarchyFromUIElement(this.GetType())) { Debug.WriteLine($"[DEBUG] {ele?.Name}"); }
    }

    void MainPageLoading(FrameworkElement sender, object args)
    {
        Logger?.WriteLine($"The MainPage is loading.", LogLevel.Debug);
        chosenDate.MinDate = new DateTimeOffset(DateTime.Now.AddYears(-10));
        chosenDate.MaxDate = new DateTimeOffset(DateTime.Now.AddYears(1));
        url.Text = "More WinUI3 examples at my github https://github.com/GuildOfCalamity?tab=repositories";
        if (useBloom)
        {
            imgGraph.Loaded += (s, e) =>
            {
                //Support.BloomHelper.AddBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), Windows.UI.Color.FromArgb(200, 150, 217, 255), 8);
                Support.BloomHelper.AddBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), Windows.UI.Color.FromArgb(200, 0, 0, 0), new System.Numerics.Vector3(3,3,0), 3);
            };
            imgGraph.Unloaded += (s, e) =>
            {
                Support.BloomHelper.RemoveBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), null);
            };
            //btnAdd.Loaded += (s, e) =>
            //{
            //    Support.BloomHelper.AddBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), Windows.UI.Color.FromArgb(220, 249, 249, 249), System.Numerics.Vector3.Zero);
            //};
            //btnAdd.Unloaded += (s, e) =>
            //{
            //    Support.BloomHelper.RemoveBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), null);
            //};
            //btnUpdate.Loaded += (s, e) =>
            //{
            //    Support.BloomHelper.AddBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), Windows.UI.Color.FromArgb(220, 249, 249, 249), System.Numerics.Vector3.Zero);
            //};
            //btnUpdate.Unloaded += (s, e) =>
            //{
            //    Support.BloomHelper.RemoveBloom((UIElement)s, Support.BloomHelper.FindParentPanel((UIElement)s), null);
            //};
        }

        #region [Code-behind event for import AutoSuggestBox]
        //SetupFileAutoSuggest(asbFilePath, Functions.DefaultDownloadPath());
        asbFilePath.QuerySubmitted += (sender, args) =>
        {
            // If the user selects a suggestion, args.ChosenSuggestion will be non-null.
            // Otherwise, you can capture the text from autoSuggestBox.Text.
            string selected = args.ChosenSuggestion as string ?? asbFilePath.Text;

            // For example, display or process the selected file name.
            Debug.WriteLine($"[INFO] User selected: {selected}");
            var final = Path.Combine(Functions.DefaultDownloadPath(), selected);
            Debug.WriteLine($"[INFO] Path imported: {final}");
            ViewModel?.ImportItemCommand.Execute(final);
        };
        #endregion
    }

    void ItemListView_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Most of the DataTemplate in the Grid are TextBlocks, but you can adjust this to any control you want.
        var source = e.OriginalSource as TextBlock;
        Debug.WriteLine($"[INFO] Right-clicked \"{(source != null ? source.Text : "null")}\"");

        var dc = ((FrameworkElement)e.OriginalSource).DataContext;
        if (dc is not null)
            ViewModel?.RightClickedCommand.Execute((ExpenseItem)dc);
    }

    void ItemListView_Loaded(object sender, RoutedEventArgs e)
    {
        return;
        #region [For testing, can be removed]
        var dlv = (ListView)sender;

        // Selecting the last item.
        var items = dlv.ItemsSource as IEnumerable<ExpenseItem>;
        var item = items?.LastOrDefault();
        if (item != null)
        {
            //dlv.SelectedItem = item;
            dlv.ScrollIntoView(item);
            ((ListViewItem)dlv.ContainerFromItem(item))?.Focus(FocusState.Keyboard);
        }

        // Scroll through all items.
        Task.Run(async () =>
        {
            if (items != null)
            {
                await Task.Delay(500);
                foreach (var di in items)
                {
                    await Task.Delay(50);
                    dlv.DispatcherQueue.TryEnqueue(() =>
                    {
                        dlv.SelectedItem = di;
                        dlv.ScrollIntoView(di);
                        ((ListViewItem)dlv.ContainerFromItem(di))?.Focus(FocusState.Programmatic);
                    });
                }
            }

        });
        #endregion
    }

    void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //VisualStateManager.GoToState(sender as Control, "Selected", false);

        // The IList<T> generic interface is a descendant of the ICollection<T>
        // generic interface and is the base interface of all generic lists.
        var list = e.AddedItems as IList<object>;

        // If there was no data model in the ItemTemplate...
        //foreach (ListViewItem item in TestView.Items) { Debug.WriteLine($"[ItemType] {item.GetType()}"); }

        // There could be multiple items in the IList, e.g. if SelectionMode="Multiple".
        foreach (var item in list)
        {
            if (item is ExpenseItem di)
            {
                // You could also set the selected ExpenseItem from here.
                //ViewModel.SelectedItem = di;
            }
        }
    }

    /// <summary>
    /// Setup a <see cref="CommandBarFlyout"/> for each <see cref="ExpenseItem"/>.
    /// </summary>
    void ExpenseItemBorder_Loaded(object sender, RoutedEventArgs e)
    {
        DependencyObject item = VisualTreeHelper.GetParent(sender as Border);
        
        // Walk the parents until we find a ListViewItem.
        while (item is not ListViewItem)
        {
            item = VisualTreeHelper.GetParent(item);
        }

        // When an item is right-clicked our CommandBarFlyout will appear.
        if (item is ListViewItem lvi)
            lvi.ContextFlyout = eiFlyout;
    }

    void ItemListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // We can check if the user has ctrl-clicked or shift-clicked.
        var shiftPress = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
        var ctrlPress = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

        if ((e.OriginalSource as FrameworkElement)?.DataContext is ExpenseItem ei)
        {
            //ViewModel?.RemoveItemCommand.Execute(ei);
        }
    }

    /// <summary>
    /// Thread-safe helper for <see cref="Microsoft.UI.Xaml.Controls.InfoBar"/>.
    /// </summary>
    /// <param name="message">text to show</param>
    /// <param name="severity"><see cref="Microsoft.UI.Xaml.Controls.InfoBarSeverity"/></param>
    public void ShowMessage(string message, InfoBarSeverity severity)
    {
        infoBar.DispatcherQueue?.TryEnqueue(() =>
        {
            infoBar.IsOpen = true;
            infoBar.Severity = severity;
            infoBar.Message = $"{message}";
        });
    }

    #region [AutoSuggestBox]
    void OnTypingPaused(object sender, EventArgs e) => DisplaySuggestions(sender as AutoSuggestBox);

    /// <summary>
    /// <see cref="AutoSuggestBox"/> behavior event when the user pauses whilst typing.
    /// </summary>
    /// <remarks>
    /// Our search parses the <see cref="ExpenseItem.Description"/> and <see cref="ExpenseItem.Codes"/>.
    /// </remarks>
    void DisplaySuggestions(AutoSuggestBox? sender)
    {
        if (sender == null || ViewModel == null)
            return;

        ViewModel.IsBusy = true;
        List<ExpenseItem>? suitableItems = new();
        string[]? splitText = sender.Text.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var ei in ViewModel.ExpenseItems)
        {
            var name = $"{ei.Description} {ei.Codes}";
            
            if (string.IsNullOrEmpty(name))
                continue;

            // LINQ "splitText.All(Func<string, bool>)"
            var found = splitText.All((key) => { return name.Contains(key, StringComparison.OrdinalIgnoreCase); });

            if (found)
                suitableItems.Add(ei);
        }

        //if (suitableItems.Count == 0)
        //    suitableItems.Add("No matching result found");

        sender.ItemsSource = suitableItems;
        ViewModel.IsBusy = false;
    }

    /// <summary>
    /// <see cref="AutoSuggestBox"/> event when the user click a suggestion from the list.
    /// </summary>
    void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selected = args.SelectedItem as ExpenseItem;

        if (selected is null || ViewModel is null)
        {
            Debug.WriteLine($"[WARNING] OnSuggestionChosen: Selected item is invalid.");
            return;
        }
        //_ = App.ShowDialogBox($"Selection", $"{selected}", "OK", "", null, null, ViewModel._dialogImgUri2);
        SetSelectedItem(selected);
    }

    public void SetSelectedItem(ExpenseItem? item)
    {
        if (item is null)
            return;

        ItemListView.SelectedItem = item;
        ItemListView.ScrollIntoView(item);
        var listViewItem = ItemListView.ContainerFromItem(item) as ListViewItem;
        if (listViewItem != null)
        {   // For this effect to work properly, set the FocusVisualKind in App.xaml.cs
            // e.g. "this.FocusVisualKind = FocusVisualKind.Reveal;"
            listViewItem.Focus(FocusState.Keyboard); // don't use FocusState.Programmatic
        }
    }

    /// <summary>
    /// For when user clicks the find icon or presses [Enter].
    /// </summary>
    /// <remarks>
    /// Our search parses the <see cref="ExpenseItem.Description"/> and <see cref="ExpenseItem.Codes"/>.
    /// </remarks>
    void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (sender == null || ViewModel == null)
            return;

        var selected = args.ChosenSuggestion as ExpenseItem;
        if (selected != null)
        {
            //_ = App.ShowDialogBox($"Selection", $"{selected}", "OK", "", null, null, ViewModel._dialogImgUri2);
            ItemListView.SelectedItem = selected;
            ItemListView.ScrollIntoView(selected);
            var listViewItem = ItemListView.ContainerFromItem(selected) as ListViewItem;
            if (listViewItem != null)
            {
                // For this effect to work properly, set the FocusVisualKind in App.xaml.cs
                // e.g. "this.FocusVisualKind = FocusVisualKind.Reveal;"
                listViewItem.Focus(FocusState.Keyboard); // don't use programmatic
            }
        }
        else
        {
            ViewModel.IsBusy = true;
            List<ExpenseItem>? suitableItems = new();
            string[]? splitText = sender.Text.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            foreach (var ei in ViewModel.ExpenseItems)
            {
                var name = $"{ei.Description} {ei.Codes}";

                if (string.IsNullOrEmpty(name))
                    continue;

                // LINQ "splitText.All(Func<string, bool>)"
                var found = splitText.All((key) => { return name.Contains(key, StringComparison.OrdinalIgnoreCase); });

                if (found)
                    suitableItems.Add(ei);
            }

            //if (suitableItems.Count == 0)
            //    suitableItems.Add("No matching result found");

            sender.ItemsSource = suitableItems;
            ViewModel.IsBusy = false;
        }
    }
    #endregion

    void OnImportTypingPaused(object sender, EventArgs e) => DisplayImportSuggestions(sender as AutoSuggestBox);

    /// <summary>
    /// <see cref="AutoSuggestBox"/> behavior event when the user pauses whilst typing.
    /// </summary>
    /// <remarks>
    /// Our search parses the downloads folder for the relevant file name(s).
    /// </remarks>
    void DisplayImportSuggestions(AutoSuggestBox? sender)
    {
        if (sender == null || ViewModel == null)
            return;

        ViewModel.IsBusy = true;

        string input = sender.Text?.Trim() ?? string.Empty;
        string directoryPath = Functions.DefaultDownloadPath();
        string filePrefix = input;

        // If the input is an absolute path, extract directory and file portion.
        if (Path.IsPathRooted(input))
        {
            string potentialDir = Path.GetDirectoryName(input) ?? string.Empty;
            string potentialFilePrefix = Path.GetFileName(input) ?? string.Empty;

            if (!string.IsNullOrEmpty(potentialDir) && Directory.Exists(potentialDir))
            {
                directoryPath = potentialDir;
                filePrefix = potentialFilePrefix;
            }
            else
            {
                // If the directory part of the input isn't valid, fallback to
                // the default directory and use entire input as the filter.
                directoryPath = Functions.DefaultDownloadPath();
                filePrefix = input;
            }
        }

        try
        {
            // Use a wildcard pattern to filter file names starting with filePrefix.
            var fileSuggestions = Directory.GetFiles(directoryPath, filePrefix + "*").Select(Path.GetFileName).ToList();
            sender.ItemsSource = fileSuggestions;
        }
        catch (Exception)
        {
            // In case of an exception (for example, permission issues), clear the list.
            sender.ItemsSource = null;
        }
        ViewModel.IsBusy = false;
    }

    /// <summary>
    /// Sets up an AutoSuggestBox to display file names (from the specified directory)
    /// that start with the text entered by the user.
    /// </summary>
    /// <param name="autoSuggestBox">The AutoSuggestBox control to configure.</param>
    /// <param name="defaultDirectory">The base folder from which to retrieve file names.</param>
    void SetupFileAutoSuggest(AutoSuggestBox autoSuggestBox, string defaultDirectory)
    {
        if (!Directory.Exists(defaultDirectory))
        {
            Debug.WriteLine($"[WARNING] The folder '{defaultDirectory}' does not exist, skipping control setup.");
            return;
        }

        autoSuggestBox.TextChanged += (sender, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string input = autoSuggestBox.Text?.Trim() ?? string.Empty;
                string directoryPath = defaultDirectory;
                string filePrefix = input;

                // If the input is an absolute path, extract directory and file portion.
                if (Path.IsPathRooted(input))
                {
                    string potentialDir = Path.GetDirectoryName(input) ?? string.Empty;
                    string potentialFilePrefix = Path.GetFileName(input) ?? string.Empty;

                    if (!string.IsNullOrEmpty(potentialDir) && Directory.Exists(potentialDir))
                    {
                        directoryPath = potentialDir;
                        filePrefix = potentialFilePrefix;
                    }
                    else
                    {
                        // If the directory part of the input isn't valid, fallback to
                        // the default directory and use entire input as the filter.
                        directoryPath = defaultDirectory;
                        filePrefix = input;
                    }
                }

                try
                {
                    // Use a wildcard pattern to filter file names starting with filePrefix.
                    var fileSuggestions = Directory.GetFiles(directoryPath, filePrefix + "*").Select(Path.GetFileName).ToList();
                    autoSuggestBox.ItemsSource = fileSuggestions;
                }
                catch (Exception)
                {
                    // In case of an exception (for example, permission issues), clear the list.
                    autoSuggestBox.ItemsSource = null;
                }
            }
        };


        // Optionally, you can handle the QuerySubmitted event to act when the user makes a selection:
        autoSuggestBox.QuerySubmitted += (sender, args) =>
        {
            // If the user selects a suggestion, args.ChosenSuggestion will be non-null.
            // Otherwise, you can capture the text from autoSuggestBox.Text.
            string selected = args.ChosenSuggestion as string ?? autoSuggestBox.Text;

            // For example, display or process the selected file name.
            System.Diagnostics.Debug.WriteLine($"[INFO] User selected: {selected}");

            ViewModel?.ImportItemCommand.Execute((AutoSuggestBox)sender);
        };
    }
}

/// <summary>
/// Static helpers for XAML calling.
/// </summary>
public static class Functions
{
    public static string IdFormatter(int id) => $"ID #{id}";
    public static string DefaultDownloadPath() => $"{Windows.Storage.UserDataPaths.GetDefault().Downloads}";
}