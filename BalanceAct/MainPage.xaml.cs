using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using BalanceAct.Models;
using BalanceAct.Services;
using BalanceAct.ViewModels;

namespace BalanceAct;

public sealed partial class MainPage : Page
{
    MainViewModel? ViewModel = App.Current.Services.GetService<MainViewModel>();
    FileLogger? Logger = (FileLogger?)App.Current.Services.GetService<ILogger>();

    public MainPage()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");
        InitializeComponent();
        this.Loading += MainPageLoading;

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
            ((ListViewItem)dlv.ContainerFromItem(item))?.Focus(FocusState.Programmatic);
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
}