using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanceAct.Models;

public class ExpenseItem : ICloneable
{
    /// <summary>
    /// The unique index of the record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Is the expense recurring?
    /// </summary>
    public bool Recurring { get; set; }

    /// <summary>
    /// The category of the expense.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// The description of the expense.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The order ID or confirmation code of the expense.
    /// </summary>
    public string? Codes { get; set; }

    /// <summary>
    /// The amount of the expense.
    /// </summary>
    public string? Amount { get; set; }

    /// <summary>
    /// Time of the expense.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Bar opacity.
    /// </summary>
    public double Opacity { get; set; }

    /// <summary>
    /// The color associated with the expense.
    /// </summary>
    public Windows.UI.Color Color { get; set; }

    /// <summary>
    /// Support for deep-copy routines.
    /// </summary>
    public object Clone()
    {
        return this.MemberwiseClone();
    }

    public override string ToString()
    {
        //string format = "Description:{0,-30} Amount:{1,-20} Color:{2,-10} Date:{3,-10} Category:{4,-20} Codes:{5,-10}";
        //return String.Format(format, $"{Description}", $"{Amount}", $"{Color}", $"{Date}", $"{Category}", $"{Codes}");
        return $"{Category}{Environment.NewLine}{Description}{Environment.NewLine}{Amount}{Environment.NewLine}{Date?.ToString("ddd, dd MMM yyyy")}{Environment.NewLine}";
    }
}

/// <summary>
/// This makes the deserialization of the ExpenseItem class more performant so it doesn't have to use reflection.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(List<ExpenseItem>))]
internal sealed partial class ExpenseItemContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
