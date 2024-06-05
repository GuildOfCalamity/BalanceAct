using System;
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
using System.Globalization;


namespace BalanceAct.ViewModels;

/// <summary>
/// https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth
/// </summary>
public class MainViewModel : ObservableRecipient
{
    #region [Props]
    bool _loaded = false;
    System.Globalization.NumberFormatInfo _formatter;
    static Uri _dialogImgUri = new Uri($"ms-appx:///Assets/Warning.png");
    static Uri _dialogImgUri2 = new Uri($"ms-appx:///Assets/Info.png");
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
            if (value != null && _loaded)
            {
                SelectedId = value.Id;
                SelectedRecurring = value.Recurring;
                SelectedDescription = value.Description;
                SelectedAmount = value.Amount;
                SelectedCategory = value.Category;
                SelectedCodes = value.Codes;
                SelectedDate = value.Date;
                #region [Auto-select the appropriate ComboBox item]
                bool matched = false;
                for (int i = 0; i < Categories.Count; i++)
                {
                    if (!string.IsNullOrEmpty(value.Category) && Categories[i].Contains(value.Category, StringComparison.CurrentCultureIgnoreCase))
                    {
                        matched = true;
                        CategorySelectedIndex = i;
                        break;
                    }
                }
                if (!matched)
                    CategorySelectedIndex = 18; // Undefined
                #endregion
            }
        }
    }

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
        "Undefined",
    };

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

    bool _selectedRecurring = false;
    public bool SelectedRecurring
    {
        get => _selectedRecurring;
        set => SetProperty(ref _selectedRecurring, value);
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

    string? _importPathCSV = "";
    public string? ImportPathCSV
    {
        get => _importPathCSV;
        set => SetProperty(ref _importPathCSV, value);
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

    string? _projectedYearTotal = "$0.00";
    public string? ProjectedYearTotal
    {
        get => _projectedYearTotal;
        set => SetProperty(ref _projectedYearTotal, value);
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

    #endregion

    #region [Relay Commands]
    public ICommand AddItemCommand { get; }
    public ICommand UpdateItemCommand { get; }
    public ICommand ImportItemCommand { get; }
    #endregion

    FileLogger? Logger = (FileLogger?)App.Current.Services.GetService<ILogger>();
    DataService? dataService = (DataService?)App.Current.Services.GetService<IDataService>();

    public MainViewModel()
    {
        Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{System.Reflection.MethodBase.GetCurrentMethod()?.Name} [{DateTime.Now.ToString("hh:mm:ss.fff tt")}]");

        // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo?view=net-8.0
        _formatter = System.Globalization.NumberFormatInfo.CurrentInfo;

        if (App.LocalConfig is not null)
            Status = "Loading…";
        else
            Status = "No configuration ⚠️";

        if (Logger is not null)
            Logger.OnException += (error) => _ = App.ShowDialogBox($"Logger", $"{error}{Environment.NewLine}", "OK", "", null, null, _dialogImgUri);

        #region [Add Action]
        AddItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                await Task.Delay(750); // for spinners

                if (SelectedDate is null || string.IsNullOrEmpty(SelectedCategory) || string.IsNullOrEmpty(SelectedDescription) || string.IsNullOrEmpty(SelectedAmount))
                {
                    Status = $"Category, Date, Description & Amount must contain some value ⚠️";
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
                    var highest = GetHighestId();

                    ExpenseItems.Add(new ExpenseItem
                    {
                        Recurring = SelectedRecurring,
                        Id = highest + 1,
                        Category = $"{SelectedCategory}",
                        Description = $"{SelectedDescription}",
                        Amount = SelectedAmount.StartsWith("$") ? $"{SelectedAmount}" : $"${SelectedAmount}",
                        Codes = string.IsNullOrEmpty(SelectedCodes) ? $"" : $"{SelectedCodes}",
                        Date = (SelectedDate is null) ? DateTime.Now : SelectedDate.Value.DateTime,
                        Color = Microsoft.UI.Colors.WhiteSmoke
                    });

                    // If we've changed something then we should update our totals.
                    UpdateSummaryTotals();

                    // Reload our data set.
                    SaveExpenseItemsJson();
                    LoadExpenseItemsJson();

                    Status = "Expense item was added ✔️";
                }
                else
                {
                    Status = "This expense item already exists, try updating instead of adding, or change more details to make it unique ⚠️";
                }
            }
            finally
            {
                IsBusy = false;
            }
        });
        #endregion

        #region [Update Action]
        UpdateItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                await Task.Delay(750); // for spinners

                if (SelectedDate is null || string.IsNullOrEmpty(SelectedCategory) || string.IsNullOrEmpty(SelectedDescription) || string.IsNullOrEmpty(SelectedAmount))
                {
                    Status = $"Category, Date, Description & Amount must contain some value ⚠️";
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
                            item.Recurring = SelectedRecurring;
                            item.Description = SelectedDescription;
                            item.Amount = SelectedAmount.StartsWith("$") ? $"{SelectedAmount}" : $"${SelectedAmount}";
                            item.Category = SelectedCategory;
                            item.Codes = string.IsNullOrEmpty(SelectedCodes) ? $"" : $"{SelectedCodes}";
                            item.Date = (SelectedDate is null) ? DateTime.Now : SelectedDate.Value.DateTime;
                        }
                    }
                }

                if (!found)
                {
                    Status = "No existing expense item could be matched, make sure you've selected one from the list ⚠️";
                }
                else
                {
                    Status = "Expense item was updated ✔️";

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
        #endregion

        #region [Import Action]
        ImportItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                await Task.Delay(500); // for spinners

                if (ImportPathCSV is null || string.IsNullOrEmpty(ImportPathCSV))
                {
                    Status = $"You must provide a valid file path ⚠️";
                    return;
                }

                int added = 0;
                string baseFolder = "";

                if (App.IsPackaged)
                    baseFolder = ApplicationData.Current.LocalFolder.Path;
                else
                    baseFolder = Directory.GetCurrentDirectory();

                if (!File.Exists(Path.Combine(baseFolder, ImportPathCSV)))
                {
                    Status = $"The file does not exist, check your input and try again ⚠️";
                    return;
                }

                var lines = Extensions.ReadFileLines(Path.Combine(baseFolder, ImportPathCSV));

                #region [Perform backup before the import]
                List<ExpenseItem> toBackup = new();
                
                foreach (var item in ExpenseItems)
                    toBackup.Add(item);
                
                var bkup = dataService?.MakeBackup(baseFolder, App.DatabaseExpense, toBackup);
                if (bkup != null && !bkup.Value) 
                {
                    Status = $"Backup attempt failed ⚠️";
                    _ = App.ShowDialogBox($"Backup", $"Unable to backup the current data set!", "OK", "", null, null, _dialogImgUri);
                    return;
                }
                #endregion

                #region [Analyze each line from the file]
                foreach (var line in lines.Skip(1)) // ignore the header
                {
                    var tokens = line.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    // Do we have enough columns to work with?
                    if (tokens.Length == 0 || tokens.Length < 4)
                    {
                        Logger?.WriteLine($"Not enough columns for this line ⇒ {line}", LogLevel.Warning);
                        continue;
                    }

                    if (double.TryParse(tokens[3], System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out double val))
                    {
                        if (val < 0)
                        {
                            Debug.WriteLine($"[INFO] Processing date line {tokens[0]}");

                            if (DateTime.TryParse($"{tokens[0]}", out DateTime impDT))
                            {
                                var impDesc = tokens[1];
                                var impCat = tokens[2];
                                var impAmnt = $"{Math.Abs(val)}"; // tokens[3]
                                var impMemo = (tokens.Length == 5) ? tokens[4] : "";

                                #region [Try to extrapolate memo]
                                // Certain banks use different delimiters, so you may have to adjust as needed.
                                if (string.IsNullOrEmpty(impMemo))
                                {
                                    // 1st common delimiter
                                    if (!string.IsNullOrEmpty(impDesc) && impDesc.Contains("*"))
                                    {
                                        try
                                        {
                                            impMemo = impDesc.Split('*', StringSplitOptions.RemoveEmptyEntries)[1];
                                            impDesc = impDesc.Split('*', StringSplitOptions.RemoveEmptyEntries)[0];
                                        }
                                        catch (Exception)
                                        {
                                            Status = $"Failed to auto-apply memo: \"{tokens[1]}\" ⚠️";
                                        }
                                    }
                                    // 2nd common delimiter
                                    else if (!string.IsNullOrEmpty(impDesc) && impDesc.Contains("#"))
                                    {
                                        try
                                        {
                                            impMemo = impDesc.Split('#', StringSplitOptions.RemoveEmptyEntries)[1];
                                            impDesc = impDesc.Split('#', StringSplitOptions.RemoveEmptyEntries)[0];
                                        }
                                        catch (Exception)
                                        {
                                            Status = $"Failed to auto-apply memo: \"{tokens[1]}\" ⚠️";
                                        }
                                    }
                                    // 3rd common delimiter
                                    else if (!string.IsNullOrEmpty(impDesc) && impDesc.Contains("~"))
                                    {
                                        try
                                        {
                                            impMemo = impDesc.Split('~', StringSplitOptions.RemoveEmptyEntries)[1];
                                            impDesc = impDesc.Split('~', StringSplitOptions.RemoveEmptyEntries)[0];
                                        }
                                        catch (Exception)
                                        {
                                            Status = $"Failed to auto-apply memo: \"{tokens[1]}\" ⚠️";
                                        }
                                    }
                                    // 4th common delimiter
                                    else if (!string.IsNullOrEmpty(impDesc) && impDesc.Contains("%"))
                                    {
                                        try
                                        {
                                            impMemo = impDesc.Split('%', StringSplitOptions.RemoveEmptyEntries)[1];
                                            impDesc = impDesc.Split('%', StringSplitOptions.RemoveEmptyEntries)[0];
                                        }
                                        catch (Exception)
                                        {
                                            Status = $"Failed to auto-apply memo: \"{tokens[1]}\" ⚠️";
                                        }
                                    }
                                }
                                #endregion

                                bool duplicate = false;
                                foreach (var item in ExpenseItems)
                                {
                                    if (AreDatesEqualish(item.Date, impDT) && 
                                        impCat.Equals(item.Category, StringComparison.OrdinalIgnoreCase) && 
                                        impDesc.Equals(item.Description, StringComparison.OrdinalIgnoreCase) &&
                                        AreAmountsEqualish(impAmnt, item.Amount))
                                    {
                                        Logger?.WriteLine($"Expense item already exists ⇒ {line}", LogLevel.Warning);
                                        duplicate = true;
                                        break;
                                    }
                                }

                                //bool duplicate = ExpenseItems.Any(ei => AreDatesEqualish(ei.Date, impDT) && ei.Category.Equals(impCat, StringComparison.CurrentCultureIgnoreCase) && ei.Description.Equals(impDesc, StringComparison.CurrentCultureIgnoreCase) && AreAmountsEqualish(ei.Amount, impAmnt));

                                // Add the imported item.
                                if (!duplicate)
                                {
                                    added++;
                                    var highest = GetHighestId();
                                    ExpenseItems.Add(new ExpenseItem
                                    {
                                        Recurring = SelectedRecurring,
                                        Id = highest + 1,
                                        Category = $"{impCat}",
                                        Description = $"{impDesc}",
                                        Amount = impAmnt.StartsWith("$") ? $"{impAmnt}" : $"${impAmnt}",
                                        Codes = string.IsNullOrEmpty(impMemo) ? $"" : $"{impMemo}",
                                        Date = (impDT == DateTime.MinValue) ? DateTime.Now : impDT,
                                        Color = Microsoft.UI.Colors.WhiteSmoke
                                    });
                                }
                                else
                                {
                                    Logger?.WriteLine($"Expense item already exists ⇒ {line}", LogLevel.Warning);
                                    Status = $"This expense item already exists, skipping import ⚠️";
                                }
                            }
                            else
                            {
                                Status = $"This date is not valid ⇒ {tokens[0]} ⚠️";
                                _ = App.ShowDialogBox($"Import", $"Unable to use this date:{Environment.NewLine}{Environment.NewLine}\"{tokens[0]}\"", "OK", "", null, null, _dialogImgUri);
                            }
                        }
                        else
                        {
                            Status = $"Deposit was skipped, only interested in withdrawals ⚠️";
                        }
                    }
                    else
                    {
                        Status = $"Unable to use this line ⇒ {line} ⚠️";
                        Logger?.WriteLine($"Unable to use this line ⇒ {line}", LogLevel.Warning);
                    }
                }
                #endregion

                await Task.Delay(500); // for spinners

                // If we've changed something, update and save.
                if (added > 0)
                {
                    _ = App.ShowDialogBox($"Results", $"Import was successful.{Environment.NewLine}{Environment.NewLine}{added} expenses were added to the database.", "OK", "", null, null, _dialogImgUri2);
                    UpdateSummaryTotals();
                    SaveExpenseItemsJson();
                    LoadExpenseItemsJson();
                }
                else
                {
                    _ = App.ShowDialogBox($"Warning", $"Import was unsuccessful.{Environment.NewLine}No expenses were added to the database.{Environment.NewLine}Please confirm the import layout matches the suggested layout.", "OK", "", null, null, _dialogImgUri);
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
            Logger?.WriteLine($"SelectedCategory defaulted to \"{SelectedCategory}\"", LogLevel.Info);
        }
        #endregion
    }

    #region [Statistical Methods]
    public void UpdateSummaryTotals()
    {
        if (ExpenseItems.Count == 0) { return; }
        
        double cmTotal = 0;
        double pmTotal = 0;
        double ytdTotal = 0;
        double changeRate = 0;
        List<string> cats = new();
        List<double> amnts = new();

        // For recurring calculations.
        var msboy = MonthsSinceBeginningOfYear();

        foreach (var item in ExpenseItems)
        {
            // Only update totals if we have a valid amount.
            if (double.TryParse(item.Amount, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out double val))
            {
                amnts.Add(val);

                if (item.Recurring)
                {
                    // Year To Date ($)
                    ytdTotal += val * msboy;

                    // Consider it spent already for the current month.
                    cmTotal += val;

                    // Consider it spent for the previous month.
                    pmTotal += val;
                }
                else
                {
                    // Year To Date ($)
                    ytdTotal += val;

                    // Current Month ($)
                    if (((DateTime)item.Date!).WithinAmountOfDays(DateTime.Now, 30))
                        cmTotal += val;
                    else if (((DateTime)item.Date!).WithinAmountOfDays(DateTime.Now, 60))
                        pmTotal += val;
                }

                cats.Add(item.Category);
            }
            else
            {
                Status = $"Amount could not be parsed ⇒ \"{item.Amount}\" ⚠️";
                _ = App.ShowDialogBox($"Warning", $"{nameof(ExpenseItem)} amount could not be parsed ⇒ \"{item.Amount}\"", "OK", "", null, null, _dialogImgUri);
            }
        }

        // Exclude outliers in calculation.
        var meanResult = CalculateMedianAdjustable(amnts, ExpenseItems.Count / 2);

        List<KeyValuePair<string, int>> grouped = CountAndSortCategories(cats);

        // TODO: Account for recurring
        var months = CountUniqueMonths(ExpenseItems.ToList());

        try
        {   // Previous Month (%)
            changeRate = CalculatePercentageChange(pmTotal, cmTotal);
            
            //AverageExpense = (ytdTotal / CurrentCount).ToString("C2", _formatter);
            AverageExpense = meanResult.ToString("C2", _formatter);
            
            // TODO: Add median calculation.
            AveragePerMonth = (ytdTotal / months).ToString("C2", _formatter);
        }
        catch (DivideByZeroException ex)
        {
            Status = $"{ex.Message} ⚠️";
        }

        // Update observable properties.
        ProjectedYearTotal = (cmTotal * 12d).ToString("C2", _formatter);
        CurrentMonthTotal = cmTotal.ToString("C2", _formatter);
        YearToDateTotal = ytdTotal.ToString("C2", _formatter);
        // NOTE: A percent sign (%) in a format string causes a number to be multiplied by 100 before it is formatted.
        // The localized percent symbol is inserted in the number at the location where the % appears in the format string.
        // This is why you'll see the (value/100) before it is assigned.
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#percent-format-specifier-p
        PreviousMonthChange = (changeRate / 100).ToString("P1", _formatter);
        if (grouped.Count > 0)
            FrequentCategory = grouped[0].Key;
    }

    public double CalculatePercentageChange(double previous, double current)
    {
        if (previous == 0)
            throw new DivideByZeroException("⚠️ Previous month's spending cannot be zero.");

        return ((current - previous) / previous) * 100;
    }

    public List<KeyValuePair<string, int>> CountAndSortCategories(List<string> categories)
    {
        try
        {
            var categoryCounts = categories
                .GroupBy(category => category)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .OrderByDescending(pair => pair.Value)
                .ToList();

            return categoryCounts;
        }
        catch (Exception ex)
        {
            _ = App.ShowDialogBox($"Warning", $"CalculatePercentageChange ⇒ \"{ex.Message}\"", "OK", "", null, null, _dialogImgUri);
            return new List<KeyValuePair<string, int>>();
        }
    }

    /// <summary>
    /// This LINQ query effectively counts the number of unique months in your data set by 
    /// considering both the year and the month of each DateTime property.
    /// </summary>
    /// <param name="elements"><see cref="List{T}"/></param>
    public int CountUniqueMonths(List<ExpenseItem> elements)
    {
        if (elements is null || elements.Count == 0)
            return 0;

        var uniqueMonths = elements
            .Select(e => new { e.Date!.Value.Year, e.Date!.Value.Month })
            .Distinct()
            .Count();

        return uniqueMonths;
    }

    /// <summary>
    /// monthsPassed = (now.Year - beginningOfYear.Year) * 12 + now.Month - beginningOfYear.Month
    /// </summary>
    public int MonthsSinceBeginningOfYear()
    {
        DateTime now = DateTime.Now;
        DateTime beginningOfYear = new DateTime(now.Year, 1, 1);
        int monthsPassed = (now.Year - beginningOfYear.Year) * 12 + now.Month - beginningOfYear.Month;
        return monthsPassed;
    }

    /// <summary>
    /// A more accurate averaging method by removing the outliers.
    /// </summary>
    public double CalculateMedianAdjustable(List<double> values, int sampleCount)
    {
        if (values == null || values.Count == 0 || sampleCount <= 0)
            return 0d;

        values.Sort();

        int count = values.Count;

        if (sampleCount >= count && count > 2)
            sampleCount = count - 2;
        else if (sampleCount >= count && count <= 2)
            sampleCount = count - 1;

        // Calculate the starting index of the middle elements
        int startIndex = Math.Abs((count - sampleCount) / 2);

        // Get the middle elements
        var middleElements = values.Skip(startIndex).Take(sampleCount);

        // Calculate the average of the middle elements
        double middleAverage = middleElements.Average();

        return middleAverage;
    }

    /// <summary>
    /// A more accurate averaging method by removing the outliers.
    /// </summary>
    public double CalculateMedian(List<double> values)
    {
        if (values == null || values.Count == 0)
            return 0d;

        values.Sort();

        // Find the middle index
        int count = values.Count;
        double medianAverage;

        if (count % 2 == 0)
        {   // Even number of elements: average the two middle elements
            int mid1 = count / 2 - 1;
            int mid2 = count / 2;
            medianAverage = (values[mid1] + values[mid2]) / 2.0;
        }
        else
        {   // Odd number of elements: take the middle element
            int mid = count / 2;
            medianAverage = values[mid];
        }

        return medianAverage;
    }
    #endregion

    #region [Helper Methods]
    public bool AreDatesEqualish(DateTime? date1, DateTime date2)
    {
        if (date1 is null)
            return false;

        return date1.Value.Year == date2.Year &&
               date1.Value.Month == date2.Month &&
               date1.Value.Day == date2.Day;
    }

    public bool AreAmountsEqualish(string amount1, string? amount2)
    {
        if (string.IsNullOrEmpty(amount2))
            return false;

        if (TryParseDollarAmount(amount1, out decimal value1) && TryParseDollarAmount(amount2, out decimal value2))
            return value1 == value2;

        // If either parsing fails, consider the amounts not equal
        return false;
    }

    public bool TryParseDollarAmount(string amount, out decimal value)
    {
        // Remove the dollar sign if present
        string cleanedAmount = amount.Replace("$", "").Trim();

        // Attempt to parse the cleaned amount
        return decimal.TryParse(cleanedAmount, NumberStyles.Currency, CultureInfo.InvariantCulture, out value);
    }

    public int GetHighestId()
    {
        if (ExpenseItems is null || ExpenseItems.Count == 0) { return 0; }

        int highest = 0;
        foreach (var item in ExpenseItems)
        {
            if (item.Id > highest)
                highest = item.Id;
        }

        return highest;
    }

    void ShowStatusMessage(string message)
    {
        Status = $"{message} ✔️";
        // Cycle prop to re-trigger AutoInfoBar.
        Show = false; Show = true;
    }

    /// <summary>
    /// Creates a new <see cref="ObservableCollection{T}"/> with example data.
    /// </summary>
    /// <returns><see cref="ObservableCollection{T}"/></returns>
    ObservableCollection<ExpenseItem> GenerateDefaultItems()
    {
        return new ObservableCollection<ExpenseItem>
        {
            new ExpenseItem { Id = 1, Opacity = 1d, Description = $"💰 A sample food expense item", Date = DateTime.Now.AddDays(-2),  Amount = "$54.62", Category = "Grocery", Recurring = false, Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 2, Opacity = 1d, Description = $"💰 A sample insurance expense item", Date = DateTime.Now.AddDays(-10), Amount = "$321.78", Category = "Insurance", Codes = "Policy #123456", Recurring = true, Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 3, Opacity = 1d, Description = $"💰 A sample gas expense item", Date = DateTime.Now.AddDays(-20), Amount = "$99.34", Category = "Fuel", Recurring = false, Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 4, Opacity = 1d, Description = $"💰 A sample entertainment expense item", Date = DateTime.Now.AddDays(-31), Amount = "$12.00", Category = "Entertainment", Recurring = false, Codes = "CHK#2112", Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 5, Opacity = 1d, Description = $"💰 A sample travel expense item", Date = DateTime.Now.AddDays(-62), Amount = "$223.11", Category = "Travel", Recurring = false, Codes = "Confirmation QRZ9981", Color = Microsoft.UI.Colors.WhiteSmoke },
        };
    }
    #endregion

    #region [Bound Events]
    /// <summary>
    /// <see cref="ComboBox"/> event for category.
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
                _ = App.ShowDialogBox($"Exception", $"CategorySelectionChanged: {ex.Message}", "OK", "", null, null, _dialogImgUri);
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
            Status = $"Loaded {ExpenseItems.Count} expense items ✔️";
        else
            Status = $"No data available ⚠️";
        #endregion

        _loaded = true;
        UpdateSummaryTotals();
    }
    #endregion

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
                    Status = $"JSON data was null ({App.DatabaseExpense}) ⚠️";
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
            Status = $"LoadExpenseItemsJson: {ex.Message} ⚠️";
            Logger?.WriteLine($"{ex.Message}", LogLevel.Error);
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
                    Status = $"No {nameof(ExpenseItem)}s to save ⚠️";
                    _ = App.ShowMessageBox("SaveExpenseItems", $"There are no {nameof(ExpenseItem)}s to save.", "OK", string.Empty, null, null);
                }
            }
        }
        catch (Exception ex)
        {
            if (!App.IsClosing)
                Status = $"SaveExpenseItemsJson: {ex.Message} ⚠️";

            Logger?.WriteLine($"{ex.Message}", LogLevel.Error);
            Debugger.Break();
        }
        finally
        {
            if (!App.IsClosing)
                IsBusy = false;
        }
    }
    #endregion

    #region [Thread-safe SyncContext Test]
    Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext? _context;
    void TestDispatcherQueueSynchronizationContext(FrameworkElement fe)
    {
        var dis = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dis is not null)
        {
            _context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(dis);
            SynchronizationContext.SetSynchronizationContext(_context);

            // SynchronizationContext's Post() is the asynchronous method used in marshaling the delegate to the UI thread.
            _context?.Post(_ => fe.Height = 40, null);

            // SynchronizationContext's Send() is the synchronous method used in marshaling the delegate to the UI thread.
            _context?.Send(_ => fe.Height = 40, null);
        }
        else
        {
            // You could also use the control's dispatcher for UI calls.
            fe.DispatcherQueue.TryEnqueue(() => fe.Height = 40);
        }
    }
    #endregion
}

