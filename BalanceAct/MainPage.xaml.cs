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
using System.Reflection;

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


    void OnTypingPaused(object sender, EventArgs e) => DisplaySuggestions(sender as AutoSuggestBox);

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
            var found = splitText.All((key) => 
            { 
                return name.Contains(key, StringComparison.OrdinalIgnoreCase); 
            });

            if (found)
                suitableItems.Add(ei);
        }

        //if (suitableItems.Count == 0)
        //    suitableItems.Add("No matching result found");

        sender.ItemsSource = suitableItems;
        ViewModel.IsBusy = false;
    }

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

    public List<string> NameList = new() {
          "Olivia   ","Liam       ","Emma     ","Noah       ",
          "Amelia   ","Oliver     ","Ava      ","Elijah     ",
          "Sophia   ","Lucas      ","Charlotte","Levi       ",
          "Isabella ","Mason      ","Mia      ","Asher      ",
          "Luna     ","James      ","Harper   ","Ethan      ",
          "Gianna   ","Mateo      ","Evelyn   ","Leo        ",
          "Aria     ","Jack       ","Ella     ","Benjamin   ",
          "Ellie    ","Aiden      ","Mila     ","Logan      ",
          "Layla    ","Grayson    ","Avery    ","Jackson    ",
          "Camila   ","Henry      ","Lily     ","Wyatt      ",
          "Scarlett ","Sebastian  ","Sofia    ","Carter     ",
          "Nova     ","Daniel     ","Aurora   ","William    ",
          "Chloe    ","Alexander  ","Betty    ","Amy        ",
          "Margaret ","Peggy      ","Paula    ","Steve      ",
          "Esteban  ","Stephen    ","Riley    ","Ezra       ",
          "Nora     ","Owen       ","Hazel    ","Michael    ",
          "Abigail  ","Muhammad   ","Rylee    ","Julian     ",
          "Penelope ","Hudson     ","Elena    ","Luke       ",
          "Paul     ","Johan      ","Zoey     ","Samuel     ",
          "Isla     ","Jacob      ","Eleanor  ","Lincoln    ",
          "Elizabeth","Gabriel    ","Madison  ","Jayden     ",
          "Willow   ","Luca       ","Emilia   ","Maverick   ",
          "Violet   ","David      ","Emily    ","Josiah     ",
          "Eliana   ","Elias      ","Stella   ","Jaxon      ",
          "Maya     ","Kai        ","Paisley  ","Anthony    ",
          "Everly   ","Isaiah     ","Addison  ","Eli        ",
          "Ryleigh  ","John       ","Ivy      ","Joseph     ",
          "Grace    ","Matthew    ","Hannah   ","Ezekiel    ",
          "Bella    ","Adam       ","Naomi    ","Caleb      ",
          "Zoe      ","Isaac      ","Aaliyah  ","Theodore   ",
          "Kinsley  ","Nathan     ","Lucy     ","Theo       ",
          "Delilah  ","Thomas     ","Skylar   ","Nolan      ",
          "Leilani  ","Waylon     ","Ayla     ","Ryan       ",
          "Victoria ","Easton     ","Alice    ","Roman      ",
          "Aubrey   ","Adrian     ","Savannah ","Miles      ",
          "Serenity ","Greyson    ","Autumn   ","Cameron    ",
          "Leah     ","Colton     ","Sophie   ","Landon     ",
          "Natalie  ","Santiago   ","Athena   ","Andrew     ",
          "Lillian  ","Hunter     ","Hailey   ","Jameson    ",
          "Audrey   ","Joshua     ","Eva      ","Jace       ",
          "Everleigh","Cooper     ","Kennedy  ","Dylan      ",
          "Maria    ","Jeremy     ","Natalia  ","Kingston   ",
          "Nevaeh   ","Xavier     ","Brooklyn ","Christian  ",
          "Raelynn  ","Christopher","Arya     ","Kayden     ",
          "Ariana   ","Charlie    ","Madelyn  ","Aaron      ",
          "Claire   ","Jaxson     ","Valentina","Silas      ",
          "Kris     ","Eion       ","Sadie    ","Ryder      ",
          "Gabriella","Austin     ","Ruby     ","Dominic    ",
          "Anna     ","Amir       ","Iris     ","Carson     ",
          "Charlie  ","Jordan     ","Brielle  ","Weston     ",
          "Emery    ","Micah      ","Melody   ","Rowan      ",
          "Amara    ","Beau       ","Piper    ","Declan     ",
          "Eric     ","Nick       ","Jason    ","Evan       ",
          "Quinn    ","Everett    ","Rebecca  ","Stuart     ",
          "Mark     ","Nathan     ","Gloria   ","Wilma      ",
          "Peter    ","Scott      ","Byron    ","Stephanie  ",
          "Fred     ","Frederick  ","Bill     ","Robert     ",
          "Frank    ","Jade       ","Alex     ","Bart       ",
          "Carol    ","Sarah      ","Joan     ","Jose       "
    };
}

public static class Functions
{
    public static string IdFormatter(int id) => $"ID #{id}";
}