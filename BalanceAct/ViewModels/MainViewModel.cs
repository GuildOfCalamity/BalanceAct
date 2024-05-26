﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using BalanceAct.Models;
using BalanceAct.Services;
using BalanceAct.Support;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.Storage;
using Newtonsoft.Json.Linq;


namespace BalanceAct.ViewModels;

public class MainViewModel : ObservableRecipient
{
    #region [Props]
    bool _loaded = false;
    System.Globalization.NumberFormatInfo _formatter;
    static Uri _dialogImgUri = new Uri($"ms-appx:///Assets/Warning.png");
    static DispatcherTimer? _timer;
    public event EventHandler<bool>? ItemsLoadedEvent;

    public ObservableCollection<ExpenseItem> ExpenseItems = new();
    public List<ExpenseItem> CompareItems = new List<ExpenseItem>();

    int _currentCount = 0;
    public int CurrentCount
    {
        get => _currentCount;
        set => SetProperty(ref _currentCount, value);
    }

    ExpenseItem? _selectedItem;
    public ExpenseItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            if (value != null)
            {
                SelectedId = value.Id;
                SelectedDescription = value.Description;
                SelectedAmount = value.Amount;
                SelectedCategory = value.Category;
                SelectedCodes = value.Codes;
                SelectedDate = value.Date;
                // Auto-select the appropriate ComboBox item.
                for (int i = 0; i < Categories.Count - 1; i++)
                {
                    if (!string.IsNullOrEmpty(value.Category) && Categories[i].Contains(value.Category))
                    {
                        CategorySelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }

    ExpenseItem? _scrollToItem;
    public ExpenseItem? ScrollToItem
    {
        get => _scrollToItem;
        set => SetProperty(ref _scrollToItem, value);
    }

    Thickness _borderSize;
    public Thickness BorderSize
    {
        get => _borderSize;
        set => SetProperty(ref _borderSize, value);
    }

    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    bool _show = false;
    public bool Show
    {
        get => _show;
        set => SetProperty(ref _show, value);
    }

    string _status = "Loading…";
    public string Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            // Cycle the prop to trigger the AutoCloseInfoBar.
            Show = false; Show = true;
        }
    }

    int _selectedId = 0;
    public int SelectedId
    {
        get => _selectedId;
        set
        {
            if (value != _selectedId)
                SetProperty(ref _selectedId, value);
        }
    }

    string? _selectedCategory;
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    int _categorySelectedIndex = 0;
    public int CategorySelectedIndex
    {
        get => _categorySelectedIndex;
        set
        {
            if (value != _categorySelectedIndex)
                SetProperty(ref _categorySelectedIndex, value);
        }
    }

    string? _selectedDescription;
    public string? SelectedDescription
    {
        get => _selectedDescription;
        set
        {
            if (value != _selectedDescription)
                SetProperty(ref _selectedDescription, value);
        }
    }

    string? _selectedCodes;
    public string? SelectedCodes
    {
        get => _selectedCodes;
        set 
        {
            if (value != _selectedCodes)
                SetProperty(ref _selectedCodes, value); 
        }
    }

    string? _selectedAmount;
    public string? SelectedAmount
    {
        get => _selectedAmount;
        set 
        {
            if (value != _selectedAmount)
                SetProperty(ref _selectedAmount, value);
        }
    }

    DateTimeOffset? _selectedDate;
    public DateTimeOffset? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (value != _selectedDate)
                SetProperty(ref _selectedDate, value);
        }
    }

    #region [Stats]
    string? _currentMonthTotal = "$0.00";
    public string? CurrentMonthTotal
    {
        get => _currentMonthTotal;
        set => SetProperty(ref _currentMonthTotal, value);
    }

    string? _previousMonthChange = "1%";
    public string? PreviousMonthChange
    {
        get => _previousMonthChange;
        set => SetProperty(ref _previousMonthChange, value);
    }

    string? _yearToDateTotal = "$0.00";
    public string? YearToDateTotal
    {
        get => _yearToDateTotal;
        set => SetProperty(ref _yearToDateTotal, value);
    }

    string? _frequentCategory = "N/A";
    public string? FrequentCategory
    {
        get => _frequentCategory;
        set => SetProperty(ref _frequentCategory, value);
    }

    string? _avgExpense = "$0.00";
    public string? AverageExpense
    {
        get => _avgExpense;
        set => SetProperty(ref _avgExpense, value);
    }

    string? _avgPerMonth = "$0.00";
    public string? AveragePerMonth
    {
        get => _avgPerMonth;
        set => SetProperty(ref _avgPerMonth, value);
    }
    #endregion

    public List<string> Categories { get; set; } = new() 
    { 
        "Automotive", 
        "Bills & utilities", 
        "Education",
        "Entertainment",
        "Fee & adjustments",
        "Food & drink",
        "Gas",
        "Gifts & donations",
        "Groceries",
        "Health & wellness",
        "Home",
        "Insurance",
        "Miscellaneous",
        "Personal",
        "Professional services",
        "Shopping",
        "Travel",
        "Workplace",
    };

    public Config? Config
    {
        get => App.LocalConfig;
    }
    #endregion

    public ICommand AddItemCommand { get; }
    public ICommand UpdateItemCommand { get; }

    FileLogger? Logger = (FileLogger?)App.Current.Services.GetService<ILogger>();
    DataService? dataService = (DataService?)App.Current.Services.GetService<IDataService>();

    public MainViewModel()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo?view=net-8.0
        _formatter = System.Globalization.NumberFormatInfo.CurrentInfo;

        if (App.LocalConfig is not null)
            Status = "✔️ Loading…";
        else
            Status = "⚠️ No config!";

        AddItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                await Task.Delay(950); // for spinners

                if (SelectedDate is null || string.IsNullOrEmpty(SelectedCategory) || string.IsNullOrEmpty(SelectedDescription) || string.IsNullOrEmpty(SelectedAmount))
                {
                    Status = $"⚠️ Category, Date, Description & Amount must contain some value!";
                    //_ = App.ShowDialogBox($"Warning", $"{Environment.NewLine}Category, Description & Amount must contain some value.{Environment.NewLine}", "OK", "", null, null, _dialogImgUri);
                    return;
                }

                bool duplicate = false;

                // Check for an existing ExpenseItem.
                if (SelectedItem is not null)
                {
                    foreach (var item in ExpenseItems)
                    {
                        if (item.Id == SelectedItem.Id && item.Date == SelectedDate.Value.Date && item.Category == SelectedCategory)
                        {
                            duplicate = true;
                            break;
                        }
                    }
                }

                // Add a new ExpenseItem.
                if (!duplicate)
                {
                    ExpenseItems.Add(new ExpenseItem
                    {
                        Id = CurrentCount + 1,
                        Category = $"{SelectedCategory}",
                        Description = $"{SelectedDescription}",
                        Amount = SelectedAmount.StartsWith("$") ? $"{SelectedAmount}" : $"${SelectedAmount}",
                        Codes = string.IsNullOrEmpty(SelectedCodes) ? $"N/A" : $"{SelectedCodes}",
                        Date = (SelectedDate is null) ? DateTime.Now : SelectedDate.Value.DateTime,
                        Color = Microsoft.UI.Colors.WhiteSmoke
                    });

                    // If we've changed something then we should update our totals.
                    UpdateSummaryTotals();

                    // Reload our data set.
                    SaveExpenseItemsJson();
                    LoadExpenseItemsJson();
                }
                else
                {
                    Status = "⚠️ This expense item already exists, try updating instead of adding, or change more details to make it unique.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        });

        UpdateItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                await Task.Delay(950); // for spinners

                if (SelectedDate is null || string.IsNullOrEmpty(SelectedCategory) || string.IsNullOrEmpty(SelectedDescription) || string.IsNullOrEmpty(SelectedAmount))
                {
                    Status = $"⚠️ Category, Date, Description & Amount must contain some value!";
                    //_ = App.ShowDialogBox($"Warning", $"{Environment.NewLine}Category, Description & Amount must contain some value.{Environment.NewLine}", "OK", "", null, null, _dialogImgUri);
                    return;
                }

                bool found = false;

                // Update an existing ExpenseItem.
                if (SelectedItem is not null)
                {
                    foreach (var item in ExpenseItems)
                    {
                        if (item.Id == SelectedItem.Id)
                        {
                            found = true;
                            Debug.WriteLine($"[INFO] Updating {nameof(ExpenseItem)} #{item.Id}");
                            item.Description = SelectedDescription;
                            item.Amount = SelectedAmount.StartsWith("$") ? $"{SelectedAmount}" : $"${SelectedAmount}";
                            item.Category = SelectedCategory;
                            item.Codes = string.IsNullOrEmpty(SelectedCodes) ? $"N/A" : $"{SelectedCodes}";
                            item.Date = (SelectedDate is null) ? DateTime.Now : SelectedDate.Value.DateTime;
                        }
                    }
                }

                if (!found)
                {
                    Status = "⚠️ No existing expense item could be matched, make sure you've selected one from the list.";
                }
                else
                {
                    Status = "✔️ Expense item was updated.";

                    // If we've changed something then we should update our totals.
                    UpdateSummaryTotals();

                    // Reload our data set.
                    SaveExpenseItemsJson();
                    LoadExpenseItemsJson();
                }
            }
            finally
            {
                IsBusy = false;
            }
        });

        if (string.IsNullOrEmpty(SelectedCategory))
        {
            SelectedCategory = Categories[0];
            Debug.WriteLine($"[INFO] SelectedCategory defaulted ⇒ {SelectedCategory}");
        }
    }

    public void UpdateSummaryTotals()
    {
        if (ExpenseItems.Count == 0) { return; }
        
        double cmTotal = 0;
        double pmTotal = 0;
        double ytdTotal = 0;
        double changeRate = 0;
        List<string> cats = new List<string>();

        foreach (var item in ExpenseItems)
        {
            // Only update totals if we have a valid amount.
            if (double.TryParse(item.Amount, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out double val))
            {
                // Year To Date ($)
                ytdTotal += val;

                // Current Month ($)
                if (((DateTime)item.Date!).WithinAmountOfDays(DateTime.Now, 30))
                {
                    cmTotal += val;
                }
                else if (((DateTime)item.Date!).WithinAmountOfDays(DateTime.Now, 60))
                {
                    pmTotal += val;
                }

                cats.Add(item.Category);
            }
        }
        
        var grouped = CountAndSortCategories(cats);
        var months = CountUniqueMonths(ExpenseItems.ToList());

        Debug.WriteLine($"[INFO] Previous: {pmTotal}");
        Debug.WriteLine($"[INFO] Current: {cmTotal}");
        try
        {   // Previous Month (%)
            changeRate = CalculatePercentageChange(pmTotal, cmTotal);
            AverageExpense = (ytdTotal / CurrentCount).ToString("C2", _formatter);
            AveragePerMonth = (ytdTotal / months).ToString("C2", _formatter);
        }
        catch (DivideByZeroException ex)
        {
            Status = $"{ex.Message}";
        }


        // NOTE: A percent sign (%) in a format string causes a number to be multiplied by 100 before it is formatted.
        // The localized percent symbol is inserted in the number at the location where the % appears in the format string.
        // This is why you'll see the (newValue/100) before it is assigned.
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#percent-format-specifier-p
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings#the--custom-specifier-3
        CurrentMonthTotal = cmTotal.ToString("C2", _formatter);
        YearToDateTotal = ytdTotal.ToString("C2", _formatter);
        PreviousMonthChange = (changeRate / 100).ToString("P1", _formatter);
        if (grouped.Count > 0)
            FrequentCategory = grouped[0].Key;
    }

    public double CalculatePercentageChange(double previous, double current)
    {
        if (previous == 0)
            throw new DivideByZeroException("⚠️ Previous month's spending cannot be zero!");

        return ((current - previous) / previous) * 100;
    }

    public List<KeyValuePair<string, int>> CountAndSortCategories(List<string> categories)
    {
        var categoryCounts = categories
            .GroupBy(category => category)
            .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
            .OrderByDescending(pair => pair.Value)
            .ToList();

        return categoryCounts;
    }

    public static int CountUniqueMonths(List<ExpenseItem> dataElements)
    {
        var uniqueMonths = dataElements
            .Select(e => new { e.Date!.Value.Year, e.Date!.Value.Month })
            .Distinct()
            .Count();

        return uniqueMonths;
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> object with example data.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    ObservableCollection<ExpenseItem> GenerateDefaultItems()
    {
        return new ObservableCollection<ExpenseItem>
        {
            new ExpenseItem { Id = 1, Description = $"Here is a sample expense item.", Date = DateTime.Now.AddDays(-2),  Amount = "$54.62", Category = "Grocery" },
            new ExpenseItem { Id = 2, Description = $"Here is a sample expense item.", Date = DateTime.Now.AddDays(-10), Amount = "$321.78", Category = "Insurance" },
            new ExpenseItem { Id = 3, Description = $"Here is a sample expense item.", Date = DateTime.Now.AddDays(-20), Amount = "$99.34", Category = "Fuel" },
            new ExpenseItem { Id = 4, Description = $"Here is a sample expense item.", Date = DateTime.Now.AddDays(-30), Amount = "$12.00", Category = "Entertainment" },
        };
    }

    void ShowStatusMessage(string message)
    {
        Status = $"✔️ {message}";
        // Cycle prop to re-trigger AutoInfoBar.
        Show = false; Show = true;
    }

    /// <summary>
    /// <see cref="ComboBox"/> event for sorting.
    /// </summary>
    public void CategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loaded)
        {
            try
            {
                var selection = e.AddedItems[0] as string;
                if (!string.IsNullOrEmpty(selection))
                {
                    SelectedCategory = selection;
                }
            }
            catch (Exception ex)
            {
                _ = App.ShowMessageBox("Exception", $"CategorySelectionChanged: {ex.Message}", "OK", string.Empty, null, null);
            }
        }
    }

    /// <summary>
    /// Make sure we save on exit.
    /// </summary>
    public void MainPageUnloaded(object sender, RoutedEventArgs e)
    {
        if (ExpenseItems.Count > 0)
            SaveExpenseItemsJson();
    }

    /// <summary>
    /// Make sure we save on exit.
    /// </summary>
    public void MainPageLoaded(object sender, RoutedEventArgs e)
    {
        #region [Load data]
        // For debugging purposes.
        //ExpenseItems = GenerateDefaultItems();

        if (ExpenseItems.Count == 0)
            LoadExpenseItemsJson();

        if (ExpenseItems.Count > 0)
            Status = $"✔️ Loaded {ExpenseItems.Count} expense items.";
        else
            Status = $"⚠️ No data available!";
        #endregion

        _loaded = true;
        UpdateSummaryTotals();
    }

    #region [JSON Serializer Routines]
    /// <summary>
    /// Loads the <see cref="ExpenseItem"/> collection.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="DataService"/>.
    /// </remarks>
    public void LoadExpenseItemsJson(string sortBy = "descending")
    {
        string baseFolder = "";

        if (App.IsClosing)
            return;

        try
        {
            IsBusy = true;

            if (App.IsPackaged)
                baseFolder = ApplicationData.Current.LocalFolder.Path;
            else
                baseFolder = Directory.GetCurrentDirectory();

            if (File.Exists(Path.Combine(baseFolder, App.DatabaseExpense)))
            {
                Debug.WriteLine($"[INFO] DaysUntilBackupReplaced is currently set to {dataService?.DaysUntilBackupReplaced}");

                // Use our FileService for reading/writing.
                var jdata = dataService?.Read<List<ExpenseItem>>(baseFolder, App.DatabaseExpense);
                if (jdata != null)
                {
                    // Look out for duplication bugs.
                    ExpenseItems.Clear();
                    CompareItems.Clear();

                    IOrderedEnumerable<ExpenseItem>? sorted = Enumerable.Empty<ExpenseItem>().OrderBy(x => 1);

                    // Sort and then validate each item.
                    if (sortBy.StartsWith("descending", StringComparison.OrdinalIgnoreCase))
                        sorted = jdata.Select(t => t).OrderByDescending(t => t.Date);
                    else
                        sorted = jdata.Select(t => t).OrderBy(x => 1);

                    foreach (var item in sorted)
                    {
                        ExpenseItems.Add(item);
                    }

                    // Update our compare set (we don't want a reference copy)
                    CompareItems = ExpenseItems.ToList().DeepCopy();
                    CurrentCount = ExpenseItems.Count;
                }
                else
                    Status = $"⚠️ Json data was null ({App.DatabaseExpense})";
            }
            else
            {
                // Inject some dummy data if file was not found.
                ExpenseItems = GenerateDefaultItems();
                CompareItems = GenerateDefaultItems().ToList();
                SaveExpenseItemsJson();
            }
            // Signal any listeners.
            ItemsLoadedEvent?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            // Signal any listeners.
            ItemsLoadedEvent?.Invoke(this, false);
            Status = $"⚠️ LoadExpenseItemsJson: {ex.Message}";
            App.DebugLog($"LoadExpenseItemsJson: {ex.Message}");
            Debugger.Break();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves the <see cref="ExpenseItem"/> collection.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="DataService"/>.
    /// </remarks>
    public void SaveExpenseItemsJson()
    {
        string baseFolder = "";

        try
        {
            if (!App.IsClosing)
                IsBusy = true;

            if (App.IsPackaged)
                baseFolder = ApplicationData.Current.LocalFolder.Path;
            else
                baseFolder = Directory.GetCurrentDirectory();

            if (ExpenseItems.Count > 0)
            {
                // We could use our DeepCopy() helper, but we'll
                // want to filter the current notes to see if there
                // are any that should be automatically removed.
                List<ExpenseItem> toSave = new();
                foreach (var item in ExpenseItems)
                {
                    // e.g. Don't commit items that do not have a category.
                    if (!string.IsNullOrEmpty(item.Category))
                        toSave.Add(item);
                }

                // Use our FileService for reading/writing.
                dataService?.Save(baseFolder, App.DatabaseExpense, toSave);

                CurrentCount = toSave.Count;

                if (!App.IsClosing)
                {
                    // Update our compare set.
                    CompareItems.Clear();
                    CompareItems = toSave.DeepCopy();
                }
            }
            else
            {
                if (!App.IsClosing)
                {
                    Status = $"⚠️ No {nameof(ExpenseItem)}s to save.";
                    _ = App.ShowMessageBox("SaveExpenseItems", $"There are no {nameof(ExpenseItem)}s to save.", "OK", string.Empty, null, null);
                }
            }
        }
        catch (Exception ex)
        {
            if (!App.IsClosing)
                Status = $"⚠️ SaveExpenseItemsJson: {ex.Message}";

            App.DebugLog($"SaveExpenseItemsJson: {ex.Message}");
            Debugger.Break();
        }
        finally
        {
            if (!App.IsClosing)
                IsBusy = false;
        }
    }
    #endregion

    #region [SyncContext]
    Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext? _context;
    void TestDispatcherQueueSynchronizationContext(FrameworkElement fe)
    {
        var dis = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dis is not null)
        {
            _context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(dis);
            SynchronizationContext.SetSynchronizationContext(_context);

            // SynchronizationContext's Post() is the asynchronous method.
            _context?.Post(o => fe.Height = 40, null); // Marshal the delegate to the UI thread

            // SynchronizationContext's Send() is the synchronous method.
            _context?.Send(_ => fe.Height = 40, null); // Marshal the delegate to the UI thread
        }
        else
        {
            // You could also use the control's dispatcher for UI calls.
            fe.DispatcherQueue.TryEnqueue(() => fe.Height = 40);
        }
    }
    #endregion
}

