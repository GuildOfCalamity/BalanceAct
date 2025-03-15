using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using BalanceAct.Models;
using BalanceAct.Services;
using BalanceAct.ViewModels;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace BalanceAct;

public sealed partial class MainPage : Page
{
    bool useBloom = false;
    MainViewModel? ViewModel = App.Current.Services.GetService<MainViewModel>();
    FileLogger? Logger = (FileLogger?)App.Current.Services.GetService<ILogger>();

    public MainPage()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        InitializeComponent();
        this.Loading += MainPageLoading;
        ItemListView.RightTapped += ItemListView_RightTapped;
        
        foreach (var ele in Extensions.GetHierarchyFromUIElement(this.GetType()))
        {
            Debug.WriteLine($"[INFO] {ele?.Name}");
        }
    }

    void MainPageLoading(FrameworkElement sender, object args)
    {
        Logger?.WriteLine($"The MainPage is loading.", LogLevel.Debug);
        chosenDate.MinDate = new DateTimeOffset(DateTime.Now.AddYears(-10));
        chosenDate.MaxDate = new DateTimeOffset(DateTime.Now.AddYears(1));
        url.Text = "More WinUI3 examples at my github https://github.com/GuildOfCalamity?tab=repositories";
        if (useBloom)
        {
            btnAdd.Loaded += (s, e) =>
            {
                var pnl = Support.BloomHelper.FindParentPanel((UIElement)s);
                if (pnl is not null)
                    Support.BloomHelper.AddBloom((UIElement)s, pnl, Windows.UI.Color.FromArgb(220, 249, 249, 249), System.Numerics.Vector3.Zero);
            };
            btnUpdate.Loaded += (s, e) =>
            {
                var pnl = Support.BloomHelper.FindParentPanel((UIElement)s);
                if (pnl is not null)
                    Support.BloomHelper.AddBloom((UIElement)s, pnl, Windows.UI.Color.FromArgb(220, 249, 249, 249), System.Numerics.Vector3.Zero);
            };
            btnAdd.Unloaded += (s, e) =>
            {
                var pnl = Support.BloomHelper.FindParentPanel((UIElement)s);
                if (pnl is not null)
                    Support.BloomHelper.RemoveBloom((UIElement)s, pnl, null);
            };
            btnUpdate.Unloaded += (s, e) =>
            {
                var pnl = Support.BloomHelper.FindParentPanel((UIElement)s);
                if (pnl is not null)
                    Support.BloomHelper.RemoveBloom((UIElement)s, pnl, null);
            };
        }
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
                // You could also set the selected DataItem from here.
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

        if (selected == null || ViewModel == null)
        {
            ViewModel.Status = $"Selected item is invalid ⚠️";
            return;
        }

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

}

/// <summary>
/// Static helpers for XAML calling.
/// </summary>
public static class Functions
{
    public static string IdFormatter(int id) => $"ID #{id}";
    public static string DefaultDownloadPath() => $"{Windows.Storage.UserDataPaths.GetDefault().Downloads}";
}