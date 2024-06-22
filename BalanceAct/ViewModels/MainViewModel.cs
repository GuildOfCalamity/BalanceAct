﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using BalanceAct.Models;
using BalanceAct.Services;
using BalanceAct.Support;
using BalanceAct.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.Storage;
using Windows.System;

namespace BalanceAct.ViewModels;

/// <summary>
/// https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth
/// </summary>
public class MainViewModel : ObservableRecipient
{
    #region [Props]
    bool _loaded = false;
    System.Globalization.NumberFormatInfo _formatter;
    static DispatcherTimer? _timer;
    public Uri _dialogImgUri = new Uri($"ms-appx:///Assets/Warning.png");
    public Uri _dialogImgUri2 = new Uri($"ms-appx:///Assets/Info.png");
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

    Visibility _settingsVisible = Visibility.Collapsed;
    public Visibility SettingsVisible
    {
        get => _settingsVisible;
        set => SetProperty(ref _settingsVisible, value);
    }

    DelayTime _delay = DelayTime.Medium;
    public DelayTime Delay
    {
        get => _delay;
        set => SetProperty(ref _delay, value); 
    }

    bool _loading = true;
    public bool Loading
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
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

    bool _toast = false;
    public bool Toast
    {
        get => _toast;
        set
        {
            if (value)
            {
                if (Random.Shared.Next(1,11) >= 8)
                    ToastHelper.ShowWarningToast("Exception Toast", "This is a fake error, ignore me.", "ButtonNotifyLink");
                else
                    ToastHelper.ShowStandardToast("Sample Toast", "This is a sample of body text for the ToastNotification.");
            }
            SetProperty(ref _toast, value);
        }
    }

    string _status = "Loading…";
    public string Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            //Cycle the prop to trigger the AutoCloseInfoBar.
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
    public ICommand RemoveItemCommand { get; }
    public ICommand SwitchDelayCommand { get; }
    public ICommand KeyboardAcceleratorCommand { get; }
    public ICommand OpenLogCommand { get; }
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

        if (Debugger.IsAttached)
            SettingsVisible = Visibility.Visible;
        else
            SettingsVisible = Visibility.Collapsed;

        if (Logger is not null)
            Logger.OnException += (error) => _ = App.ShowDialogBox($"Logger", $"{error}{Environment.NewLine}", "OK", "", null, null, _dialogImgUri);

        #region [Add Action]
        AddItemCommand = new RelayCommand<object>(async (obj) =>
        {
            try
            {
                IsBusy = true;

                // for spinners
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(200); break;
                    case DelayTime.Medium: await Task.Delay(600); break;
                    case DelayTime.Long: await Task.Delay(2000); break;
                    default: break; // none
                }

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
                        if (AreDatesSimilar(item.Date, SelectedDate.Value.Date) && 
                            AreAmountsSimilar(item.Amount, SelectedAmount) &&
                            item.Category == SelectedCategory)
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

                // for spinners
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(200); break;
                    case DelayTime.Medium: await Task.Delay(600); break;
                    case DelayTime.Long: await Task.Delay(2000); break;
                    default: break; // none
                }

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

                // for spinners
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(200); break;
                    case DelayTime.Medium: await Task.Delay(600); break;
                    case DelayTime.Long: await Task.Delay(2000); break;
                    default: break; // none
                }

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
                var bkup = dataService?.MakeBackup(baseFolder, App.DatabaseExpense, ExpenseItems.ToList());
                if (bkup != null && !bkup.Value) 
                {
                    Status = $"Backup attempt failed ⚠️";
                    _ = App.ShowDialogBox($"Backup", $"Unable to backup the current data set!", "OK", "", null, null, _dialogImgUri);
                    return;
                }
                #endregion

                #region [Inspect column names]
                var inspection = lines.FirstOrDefault()?.Split(',', StringSplitOptions.TrimEntries);
                if (inspection == null || inspection.Length == 0)
                {
                    Status = $"No header row detected ⚠️";
                    _ = App.ShowDialogBox($"Backup", $"No header row was detected.{Environment.NewLine}Check the file contents and try again.{Environment.NewLine}{Environment.NewLine}\"{ImportPathCSV}\"", "OK", "", null, null, _dialogImgUri);
                    return;
                }

                // Configure defaults
                int colMemo = -1;
                int colDate = 0;
                int colDesc = 1;
                int colCat = 2;
                int colAmnt = 3;

                for (int i = 0; i < inspection.Length; i++)
                {
                    var col = inspection[i];

                    if (string.IsNullOrEmpty(col))
                        continue;

                    if (col.Contains("date", StringComparison.OrdinalIgnoreCase))
                        colDate = i;
                    if (col.Contains("description", StringComparison.OrdinalIgnoreCase))
                        colDesc = i;
                    if (col.Contains("category", StringComparison.OrdinalIgnoreCase))
                        colCat = i;
                    if (col.Contains("amount", StringComparison.OrdinalIgnoreCase) && !col.Contains("credit", StringComparison.OrdinalIgnoreCase))
                        colAmnt = i;
                    if (col.Contains("memo", StringComparison.OrdinalIgnoreCase) || col.Contains("additional", StringComparison.OrdinalIgnoreCase))
                        colMemo = i;
                    //if (col.Contains("check number", StringComparison.OrdinalIgnoreCase) || col.Contains("check #", StringComparison.OrdinalIgnoreCase))
                    //    colMemo = i;
                }
                Logger?.WriteLine($"Interpreted column layout ⇒ Date:{colDate}, Description:{colDesc}, Category:{colCat}, Amount:{colAmnt}, Memo:{colMemo}", LogLevel.Debug);
                #endregion

                #region [Analyze each line from the file]
                foreach (var line in lines.Skip(1)) // ignore the header
                {
                    var filtered = line.Replace("\"", "");
                    var tokens = filtered.Split(',', StringSplitOptions.TrimEntries);

                    // Do we have enough columns to work with?
                    if (tokens.Length == 0 || tokens.Length < 4)
                    {
                        Logger?.WriteLine($"Not enough columns for this line ⇒ {filtered}", LogLevel.Warning);
                        continue;
                    }

                    if (double.TryParse(tokens[colAmnt], System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out double val))
                    {
                        if (val < 0)
                        {
                            Debug.WriteLine($"[INFO] Processing ⇒ \"{filtered}\"");

                            if (DateTime.TryParse($"{tokens[colDate]}", out DateTime impDT))
                            {
                                var impDesc = tokens[colDesc];
                                var impCat = tokens[colCat];
                                var impAmnt = $"{Math.Abs(val)}";
                                var impMemo = (colMemo != -1) ? tokens[colMemo] : "";

                                #region [Try to match predefined categories]
                                foreach (var preCat in Categories)
                                {
                                    if (preCat.Contains(impCat, StringComparison.OrdinalIgnoreCase))
                                    {
                                        impCat = preCat;
                                        break;
                                    }
                                }
                                // If the category isn't matched then it will contain the native value from the file.
                                #endregion

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

                                // For the duplicate check, the memo can be amorphous, so we'll ignore it.
                                bool duplicate = ExpenseItems.Any(ei => AreDatesSimilar(ei.Date, impDT) && ei.Category.Equals(impCat, StringComparison.OrdinalIgnoreCase) && ei.Description.Equals(impDesc, StringComparison.OrdinalIgnoreCase) && AreAmountsSimilar(ei.Amount, impAmnt));

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
                                        // TODO: honor local culture ⇒ impAmnt.ToString("C2", _formatter)
                                        Amount = impAmnt.StartsWith("$") ? $"{impAmnt}" : $"${impAmnt}",
                                        Codes = string.IsNullOrEmpty(impMemo) ? "" : $"{impMemo}",
                                        Date = (impDT == DateTime.MinValue) ? DateTime.Now : impDT,
                                        Color = Microsoft.UI.Colors.WhiteSmoke
                                    });
                                }
                                else
                                {
                                    Logger?.WriteLine($"Expense item already exists ⇒ {filtered}", LogLevel.Warning);
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
                        Status = $"Unable to use this line ⇒ {filtered} ⚠️";
                        Logger?.WriteLine($"Unable to use this line ⇒ {filtered}", LogLevel.Warning);
                    }
                }
                #endregion

                // for spinners
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(200); break;
                    case DelayTime.Medium: await Task.Delay(600); break;
                    case DelayTime.Long: await Task.Delay(2000); break;
                    default: break; // none
                }

                // If we've changed something, update and save.
                if (added > 0)
                {
                    _ = App.ShowDialogBox($"Results", $"Import was successful.{Environment.NewLine}{Environment.NewLine}{added} expenses were added to the database.", "OK", "", null, null, _dialogImgUri2);
                    Logger?.WriteLine($"{added} expense items were imported into the database.", LogLevel.Info);
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
            Debug.WriteLine($"[INFO] SelectedCategory defaulted to \"{SelectedCategory}\"");
        }
        #endregion

        RemoveItemCommand = new RelayCommand<object>(async (obj) =>
        {
            if (obj != null && obj is ExpenseItem ei)
            {
                Status = $"Got item #{ei.Id}: {ei.Description} 💰";

                // for spinners
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(200); break;
                    case DelayTime.Medium: await Task.Delay(600); break;
                    case DelayTime.Long: await Task.Delay(2000); break;
                    default: break; // none
                }

                _ = App.ShowDialogBox($"Removal", $"📢 This feature is currently in development.{Environment.NewLine}", "OK", "", null, null, _dialogImgUri2);
            }
        });

        // Configure delay time command.
        SwitchDelayCommand = new RelayCommand<string>(async (param) =>
        {
            if (!string.IsNullOrEmpty(param))
            {
                if (Enum.IsDefined(typeof(DelayTime), param))
                {
                    await Task.Delay(10);
                    Delay = (DelayTime)Enum.Parse(typeof(DelayTime), param);
                }
                else
                {
                    Debug.WriteLine($"[WARNING] Parameter is not of type '{nameof(DelayTime)}'.");
                }
            }
            else
            {
                Debug.WriteLine($"[WARNING] Parameter was empty, nothing to do.");
            }
        });

        KeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteKeyboardAcceleratorCommand);

        // Log file
        OpenLogCommand = new RelayCommand<string>(async (param) =>
        {
            if (!string.IsNullOrEmpty(param))
            {
                Logger?.OpenMostRecentLog();
            }
            else
            {
                Debug.WriteLine($"[WARNING] Parameter was empty, nothing to do.");
            }
        });
    }

    /// <summary>
    /// For testing XAML KeyboardAccelerator Key="Number1" Modifiers="Control"
    /// </summary>
    /// <param name="e"><see cref="KeyboardAcceleratorInvokedEventArgs"/></param>
    void ExecuteKeyboardAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
    {
        if (e is null)
            return;

        int index = e.KeyboardAccelerator.Key switch
        {
            VirtualKey.Number1 => 1,
            VirtualKey.Number2 => 2,
            VirtualKey.Number3 => 3,
            VirtualKey.Number4 => 4,
            VirtualKey.Number5 => 5,
            VirtualKey.Number6 => 6,
            VirtualKey.Number7 => 7,
            VirtualKey.Number8 => 8,
            VirtualKey.Number9 => 9,
            _ => 0,
        };

        e.Handled = true;
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

                if (!string.IsNullOrEmpty(item.Category))
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
        if (previous.IsZero())
            throw new DivideByZeroException("Previous month's spending cannot be zero");

        return ((current - previous) / previous) * 100d;
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
    /// <summary>
    /// Compares two <see cref="DateTime"/>s ignoring the hours, minutes and seconds.
    /// </summary>
    public bool AreDatesSimilar(DateTime? date1, DateTime date2)
    {
        if (date1 is null)
            return false;

        return date1.Value.Year == date2.Year &&
               date1.Value.Month == date2.Month &&
               date1.Value.Day == date2.Day;
    }

    /// <summary>
    /// Compares two currency amounts formatted as <see cref="string"/>s.
    /// </summary>
    public bool AreAmountsSimilar(string? amount1, string amount2)
    {
        if (string.IsNullOrEmpty(amount1))
            return false;

        if (TryParseDollarAmount(amount1, out decimal value1) && TryParseDollarAmount(amount2, out decimal value2))
            return value1 == value2;

        // If either parsing fails, consider the amounts not equal.
        return false;
    }

    public bool TryParseDollarAmount(string amount, out decimal value)
    {
        // Remove the dollar sign if present
        string cleanedAmount = amount.Replace("$", "").Trim();

        // Attempt to parse the cleaned amount
        return decimal.TryParse(cleanedAmount, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out value);
        //return decimal.TryParse(cleanedAmount, NumberStyles.Currency, CultureInfo.InvariantCulture, out value);
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
            new ExpenseItem { Id = 2, Opacity = 1d, Description = $"💰 A sample insurance expense item", Date = DateTime.Now.AddDays(-10), Amount = "$300.78", Category = "Insurance", Codes = "Policy #123456", Recurring = true, Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 3, Opacity = 1d, Description = $"💰 A sample gas expense item", Date = DateTime.Now.AddDays(-20), Amount = "$99.34", Category = "Fuel", Recurring = false, Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 4, Opacity = 1d, Description = $"💰 A sample entertainment expense item", Date = DateTime.Now.AddDays(-31), Amount = "$12.00", Category = "Entertainment", Recurring = false, Codes = "CHK#2112", Color = Microsoft.UI.Colors.WhiteSmoke },
            new ExpenseItem { Id = 5, Opacity = 1d, Description = $"💰 A sample travel expense item", Date = DateTime.Now.AddDays(-62), Amount = "$223.11", Category = "Travel", Recurring = false, Codes = "Confirmation QRZ9981", Color = Microsoft.UI.Colors.WhiteSmoke },
        };
    }
    #endregion

    #region [Bound Events]
    /// <summary>
    /// <see cref="TextBox"/> keydown event for import.
    /// </summary>
    public void ImportFilePathKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (_loaded)
        {
            if (e.Key == VirtualKey.Enter)
            {
                ImportItemCommand.Execute((TextBox)sender);
                e.Handled = true;
            }
        }
    }

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
        {
            //Logger?.WriteLine($"Saving {ExpenseItems.Count} items to database.", LogLevel.Debug);
            SaveExpenseItemsJson();
        }

        Logger?.WriteLine($"The MainPage is unloading.", LogLevel.Debug);
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

        Loading = false;
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
                    if (sortBy.StartsWith("desc", StringComparison.OrdinalIgnoreCase))
                        sorted = jdata.Select(t => t).OrderByDescending(t => t.Date); // newest first
                    else if (sortBy.StartsWith("asc", StringComparison.OrdinalIgnoreCase))
                        sorted = jdata.Select(t => t).OrderBy(t => t.Date);           // newest last
                    else
                        sorted = jdata.Select(t => t).OrderBy(x => 1);                // natural order

                    foreach (var item in sorted)
                        ExpenseItems.Add(item);

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
