using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Microsoft.UI;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace BalanceAct;

public static class Extensions
{
    /// <summary>
    /// Copies one <see cref="List{T}"/> to another <see cref="List{T}"/> by value (deep copy).
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    /// <remarks>
    /// If your model does not inherit from <see cref="ICloneable"/>
    /// then a manual DTO copying technique could be used instead.
    /// </remarks>
    public static List<T> DeepCopy<T>(this List<T> source) where T : ICloneable
    {
        if (source == null)
            throw new ArgumentNullException($"{nameof(source)} list cannot be null.");

        List<T> destination = new List<T>(source.Count);
        foreach (T item in source)
        {
            if (item is ICloneable cloneable)
                destination.Add((T)cloneable.Clone());
            else
                throw new InvalidOperationException($"Type {typeof(T).FullName} does not implement ICloneable.");
        }

        return destination;
    }

    /// <summary>
    /// Checks to see if a date is between two dates.
    /// </summary>
    public static bool Between(this DateTime dt, DateTime rangeBeg, DateTime rangeEnd) => dt.Ticks >= rangeBeg.Ticks && dt.Ticks <= rangeEnd.Ticks;

    /// <summary>
    /// Returns a range of <see cref="DateTime"/> objects matching the criteria provided.
    /// </summary>
    /// <example>
    /// IEnumerable<DateTime> dateRange = DateTime.Now.GetDateRangeTo(DateTime.Now.AddDays(80));
    /// </example>
    /// <param name="self"><see cref="DateTime"/></param>
    /// <param name="toDate"><see cref="DateTime"/></param>
    /// <returns><see cref="IEnumerable{DateTime}"/></returns>
    public static IEnumerable<DateTime> GetDateRangeTo(this DateTime self, DateTime toDate)
    {
        var range = Enumerable.Range(0, new TimeSpan(toDate.Ticks - self.Ticks).Days);

        return from p in range select self.Date.AddDays(p);
    }

    /// <summary>
    /// Figure out how old something is.
    /// </summary>
    /// <returns>integer amount in years</returns>
    public static int CalculateYearAge(this DateTime dateTime)
    {
        int age = DateTime.Now.Year - dateTime.Year;
        if (DateTime.Now < dateTime.AddYears(age))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// Figure out how old something is.
    /// </summary>
    /// <returns>integer amount in months</returns>
    public static int CalculateMonthAge(this DateTime dateTime)
    {
        int age = DateTime.Now.Year - dateTime.Year;
        if (DateTime.Now < dateTime.AddYears(age))
        {
            age--;
        }

        return age * 12;
    }

    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
    /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
    /// </summary>
    /// <param name="span"><see cref="TimeSpan"/></param>
    /// <param name="significantDigits">number of right side digits in output (precision)</param>
    /// <returns></returns>
    public static string ToTimeString(this TimeSpan span, int significantDigits = 3)
    {
        var format = $"G{significantDigits}";
        return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + " seconds"
                : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + " minutes"
                : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + " hours"
                : span.TotalDays.ToString(format) + " days")));
    }

    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.
    /// e.g. 420 milliseconds, 3.1 seconds, 2 minutes, 4.231 hours, etc.
    /// </summary>
    /// <param name="span"><see cref="TimeSpan"/></param>
    /// <param name="significantDigits">number of right side digits in output (precision)</param>
    /// <returns></returns>
    public static string ToTimeString(this TimeSpan? span, int significantDigits = 3)
    {
        var format = $"G{significantDigits}";
        return span?.TotalMilliseconds < 1000 ? span?.TotalMilliseconds.ToString(format) + " milliseconds"
                : (span?.TotalSeconds < 60 ? span?.TotalSeconds.ToString(format) + " seconds"
                : (span?.TotalMinutes < 60 ? span?.TotalMinutes.ToString(format) + " minutes"
                : (span?.TotalHours < 24 ? span?.TotalHours.ToString(format) + " hours"
                : span?.TotalDays.ToString(format) + " days")));
    }

    /// <summary>
    /// Display a readable sentence as to when the time will happen.
    /// e.g. "in one second" or "in 2 days"
    /// </summary>
    /// <param name="value"><see cref="TimeSpan"/>the future time to compare from now</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(this TimeSpan value)
    {
        double delta = value.TotalSeconds;
        if (delta < 60) { return value.Seconds == 1 ? "one second" : value.Seconds + " seconds"; }
        if (delta < 120) { return "a minute"; }
        if (delta < 3000) { return value.Minutes + " minutes"; } // 50 * 60
        if (delta < 5400) { return "an hour"; } // 90 * 60
        if (delta < 86400) { return value.Hours + " hours"; } // 24 * 60 * 60
        if (delta < 172800) { return "one day"; } // 48 * 60 * 60
        if (delta < 2592000) { return value.Days + " days"; } // 30 * 24 * 60 * 60
        if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
        {
            int months = Convert.ToInt32(Math.Floor((double)value.Days / 30));
            return months <= 1 ? "one month" : months + " months";
        }
        int years = Convert.ToInt32(Math.Floor((double)value.Days / 365));
        return years <= 1 ? "one year" : years + " years";
    }

    /// <summary>
    /// Display a readable sentence as to when that time happened.
    /// e.g. "5 minutes ago" or "in 2 days"
    /// </summary>
    /// <param name="value"><see cref="DateTime"/>the past/future time to compare from now</param>
    /// <returns>human friendly format</returns>
    public static string ToReadableTime(this DateTime value, bool useUTC = false)
    {
        TimeSpan ts;
        if (useUTC) { ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks); }
        else { ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks); }

        double delta = ts.TotalSeconds;
        if (delta < 0) // in the future
        {
            delta = Math.Abs(delta);
            if (delta < 60) { return Math.Abs(ts.Seconds) == 1 ? "in one second" : "in " + Math.Abs(ts.Seconds) + " seconds"; }
            if (delta < 120) { return "in a minute"; }
            if (delta < 3000) { return "in " + Math.Abs(ts.Minutes) + " minutes"; } // 50 * 60
            if (delta < 5400) { return "in an hour"; } // 90 * 60
            if (delta < 86400) { return "in " + Math.Abs(ts.Hours) + " hours"; } // 24 * 60 * 60
            if (delta < 172800) { return "tomorrow"; } // 48 * 60 * 60
            if (delta < 2592000) { return "in " + Math.Abs(ts.Days) + " days"; } // 30 * 24 * 60 * 60
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 30));
                return months <= 1 ? "in one month" : "in " + months + " months";
            }
            int years = Convert.ToInt32(Math.Floor((double)Math.Abs(ts.Days) / 365));
            return years <= 1 ? "in one year" : "in " + years + " years";
        }
        else // in the past
        {
            if (delta < 60) { return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago"; }
            if (delta < 120) { return "a minute ago"; }
            if (delta < 3000) { return ts.Minutes + " minutes ago"; } // 50 * 60
            if (delta < 5400) { return "an hour ago"; } // 90 * 60
            if (delta < 86400) { return ts.Hours + " hours ago"; } // 24 * 60 * 60
            if (delta < 172800) { return "yesterday"; } // 48 * 60 * 60
            if (delta < 2592000) { return ts.Days + " days ago"; } // 30 * 24 * 60 * 60
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }

    /// <summary>
    /// Determines if the date is a working day, weekend, or determine the next workday coming up.
    /// </summary>
    /// <param name="date"><see cref="DateTime"/></param>
    public static bool WorkingDay(this DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Determines if the date is on a weekend (i.e. Saturday or Sunday)
    /// </summary>
    /// <param name="date"><see cref="DateTime"/></param>
    public static bool IsWeekend(this DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Gets the next date that is not a weekend.
    /// </summary>
    /// <param name="date"><see cref="DateTime"/></param>
    public static DateTime NextWorkday(this DateTime date)
    {
        DateTime nextDay = date.AddDays(1);
        while (!nextDay.WorkingDay())
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    /// <summary>
    /// Determine the Next date by passing in a DayOfWeek (i.e. from this date, when is the next Tuesday?)
    /// </summary>
    public static DateTime Next(this DateTime current, DayOfWeek dayOfWeek)
    {
        int offsetDays = dayOfWeek - current.DayOfWeek;
        if (offsetDays <= 0)
        {
            offsetDays += 7;
        }
        DateTime result = current.AddDays(offsetDays);
        return result;
    }

    /// <summary>
    /// Converts a DateTime to a DateTimeOffset with the specified offset
    /// </summary>
    /// <param name="date">The DateTime to convert</param>
    /// <param name="offset">The offset to apply to the date field</param>
    /// <returns>The corresponding DateTimeOffset</returns>
    public static DateTimeOffset ToOffset(this DateTime date, TimeSpan offset) => new DateTimeOffset(date).ToOffset(offset);

    /// <summary>
    /// Accounts for once the <paramref name="date1"/> is past <paramref name="date2"/>
    /// or falls within the amount of <paramref name="days"/>.
    /// </summary>
    public static bool WithinDaysOrPast(this DateTime date1, DateTime date2, double days = 7.0)
    {
        if (date1 > date2) // Account for past-due amounts.
            return true;
        else
        {
            TimeSpan difference = date1 - date2;
            return Math.Abs(difference.TotalDays) <= days;
        }
    }

    /// <summary>
    /// Only accounts for date1 being within range of date2.
    /// </summary>
    public static bool WithinOneDay(this DateTime date1, DateTime date2)
    {
        TimeSpan difference = date1 - date2;
        return Math.Abs(difference.TotalDays) <= 1.0;
    }

    /// <summary>
    /// Only accounts for date1 being within range of date2 by some amount.
    /// </summary>
    public static bool WithinAmountOfDays(this DateTime date1, DateTime date2, double days)
    {
        TimeSpan difference = date1 - date2;
        return Math.Abs(difference.TotalDays) <= days;
    }

    public static DateTime ConvertToLastDayOfMonth(this DateTime date) => new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    /// <summary>
    /// Multiplies the given <see cref="TimeSpan"/> by the scalar amount provided.
    /// </summary>
    public static TimeSpan Multiply(this TimeSpan timeSpan, double scalar) => new TimeSpan((long)(timeSpan.Ticks * scalar));

    /// <summary>
    /// Returns the AppData path including the <paramref name="moduleName"/>.
    /// e.g. "C:\Users\UserName\AppData\Local\MenuDemo\Settings"
    /// </summary>
    public static string LocalApplicationDataFolder(string moduleName = "Settings")
    {
        var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}\\{moduleName}");
        return result;
    }

    /// <summary>
    /// Check if a file can be created in the directory.
    /// </summary>
    /// <param name="directoryPath">the directory path to evaluate</param>
    /// <returns>true if the directory is writeable, false otherwise</returns>
    public static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            using (FileStream fs = File.Create(Path.Combine(directoryPath, "test.txt"), 1, FileOptions.DeleteOnClose)) { /* no-op */ }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void PostWithComplete<T>(this SynchronizationContext context, Action<T> action, T state)
    {
        context.OperationStarted();
        context.Post(o => {
            try { action((T)o!); }
            finally { context.OperationCompleted(); }
        },
            state
        );
    }

    public static void PostWithComplete(this SynchronizationContext context, Action action)
    {
        context.OperationStarted();
        context.Post(_ => {
            try { action(); }
            finally { context.OperationCompleted(); }
        },
            null
        );
    }

    /// <summary>
    /// Helper function to calculate an element's rectangle in root-relative coordinates.
    /// </summary>
    public static Windows.Foundation.Rect GetElementRect(this Microsoft.UI.Xaml.FrameworkElement element)
    {
        try
        {
            Microsoft.UI.Xaml.Media.GeneralTransform transform = element.TransformToVisual(null);
            Windows.Foundation.Point point = transform.TransformPoint(new Windows.Foundation.Point());
            return new Windows.Foundation.Rect(point, new Windows.Foundation.Size(element.ActualWidth, element.ActualHeight));
        }
        catch (Exception)
        {
            return new Windows.Foundation.Rect(0, 0, 0, 0);
        }
    }

    public static string SeparateCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder result = new StringBuilder();
        result.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
                result.Append(' ');

            result.Append(input[i]);
        }

        return result.ToString();
    }

    public static int CompareName(this Object obj1, Object obj2)
    {
        if (obj1 is null && obj2 is null)
            return 0;

        PropertyInfo? pi1 = obj1 as PropertyInfo;
        if (pi1 is null)
            return -1;

        PropertyInfo? pi2 = obj2 as PropertyInfo;
        if (pi2 is null)
            return 1;

        return String.Compare(pi1.Name, pi2.Name);
    }

    /// <summary>
    /// Finds the contrast ratio.
    /// This is helpful for determining if one control's foreground and another control's background will be hard to distinguish.
    /// https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    /// (L1 + 0.05) / (L2 + 0.05), where
    /// L1 is the relative luminance of the lighter of the colors, and
    /// L2 is the relative luminance of the darker of the colors.
    /// </summary>
    /// <param name="first"><see cref="Windows.UI.Color"/></param>
    /// <param name="second"><see cref="Windows.UI.Color"/></param>
    /// <returns>ratio between relative luminance</returns>
    public static double CalculateContrastRatio(Windows.UI.Color first, Windows.UI.Color second)
    {
        double relLuminanceOne = GetRelativeLuminance(first);
        double relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05) / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    /// <summary>
    /// Gets the relative luminance.
    /// https://www.w3.org/WAI/GL/wiki/Relative_luminance
    /// For the sRGB colorspace, the relative luminance of a color is defined as L = 0.2126 * R + 0.7152 * G + 0.0722 * B
    /// </summary>
    /// <param name="c"><see cref="Windows.UI.Color"/></param>
    /// <remarks>This is mainly used by <see cref="Helpers.CalculateContrastRatio(Color, Color)"/></remarks>
    public static double GetRelativeLuminance(Windows.UI.Color c)
    {
        double rSRGB = c.R / 255.0;
        double gSRGB = c.G / 255.0;
        double bSRGB = c.B / 255.0;

        // WebContentAccessibilityGuideline 2.x definition was 0.03928 (incorrect)
        // WebContentAccessibilityGuideline 3.x definition is 0.04045 (correct)
        double r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow(((rSRGB + 0.055) / 1.055), 2.4);
        double g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow(((gSRGB + 0.055) / 1.055), 2.4);
        double b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow(((bSRGB + 0.055) / 1.055), 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Calculates the linear interpolated Color based on the given Color values.
    /// </summary>
    /// <param name="colorFrom">Source Color.</param>
    /// <param name="colorTo">Target Color.</param>
    /// <param name="amount">Weight given to the target color.</param>
    /// <returns>Linear Interpolated Color.</returns>
    public static Windows.UI.Color Lerp(this Windows.UI.Color colorFrom, Windows.UI.Color colorTo, float amount)
    {
        // Convert colorFrom components to lerp-able floats
        float sa = colorFrom.A, sr = colorFrom.R, sg = colorFrom.G, sb = colorFrom.B;

        // Convert colorTo components to lerp-able floats
        float ea = colorTo.A, er = colorTo.R, eg = colorTo.G, eb = colorTo.B;

        // lerp the colors to get the difference
        byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
             r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
             g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
             b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

        // return the new color
        return Windows.UI.Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// Darkens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to darken. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static Windows.UI.Color DarkerBy(this Windows.UI.Color color, float amount)
    {
        return color.Lerp(Colors.Black, amount);
    }

    /// <summary>
    /// Lightens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to lighten. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static Windows.UI.Color LighterBy(this Windows.UI.Color color, float amount)
    {
        return color.Lerp(Colors.White, amount);
    }

    /// <summary>
    /// Clamping function for any value of type <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="val">initial value</param>
    /// <param name="min">lowest range</param>
    /// <param name="max">highest range</param>
    /// <returns>clamped value</returns>
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
    }

    /// <summary>
    /// Linear interpolation for a range of floats.
    /// </summary>
    public static float Lerp(this float start, float end, float amount = 0.5F) => start + (end - start) * amount;
    public static float LogLerp(this float start, float end, float percent, float logBase = 1.2F) => start + (end - start) * (float)Math.Log(percent, logBase);

    /// <summary>
    /// Similar to <see cref="GetReadableTime(TimeSpan)"/>.
    /// </summary>
    /// <param name="timeSpan"><see cref="TimeSpan"/></param>
    /// <returns>formatted text</returns>
    public static string ToReadableString(this TimeSpan span)
    {
        var parts = new StringBuilder();
        if (span.Days > 0)
            parts.Append($"{span.Days} day{(span.Days == 1 ? string.Empty : "s")} ");
        if (span.Hours > 0)
            parts.Append($"{span.Hours} hour{(span.Hours == 1 ? string.Empty : "s")} ");
        if (span.Minutes > 0)
            parts.Append($"{span.Minutes} minute{(span.Minutes == 1 ? string.Empty : "s")} ");
        if (span.Seconds > 0)
            parts.Append($"{span.Seconds} second{(span.Seconds == 1 ? string.Empty : "s")} ");
        if (span.Milliseconds > 0)
            parts.Append($"{span.Milliseconds} millisecond{(span.Milliseconds == 1 ? string.Empty : "s")} ");

        if (parts.Length == 0) // result was less than 1 millisecond
            return $"{span.TotalMilliseconds:N4} milliseconds"; // similar to span.Ticks
        else
            return parts.ToString().Trim();
    }

    /// <summary>
    /// This should only be used on instantiated objects, not static objects.
    /// </summary>
    public static string ToStringDump<T>(this T obj)
    {
        const string Seperator = "\r\n";
        const System.Reflection.BindingFlags BindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

        if (obj is null)
            return string.Empty;

        try
        {
            var objProperties =
                from property in obj?.GetType().GetProperties(BindingFlags)
                where property.CanRead
                select string.Format("{0} : {1}", property.Name, property.GetValue(obj, null));

            return string.Join(Seperator, objProperties);
        }
        catch (Exception ex)
        {
            return $"⇒ Probably a non-instanced object: {ex.Message}";
        }
    }

    /// <summary>
    /// var stack = GeneralExtensions.GetStackTrace(new StackTrace());
    /// </summary>
    public static string GetStackTrace(StackTrace st)
    {
        string result = string.Empty;
        for (int i = 0; i < st.FrameCount; i++)
        {
            StackFrame? sf = st.GetFrame(i);
            result += sf?.GetMethod() + " <== ";
        }
        return result;
    }

    public static string Flatten(this Exception? exception)
    {
        var sb = new StringBuilder();
        while (exception != null)
        {
            sb.AppendLine(exception.Message);
            sb.AppendLine(exception.StackTrace);
            exception = exception.InnerException;
        }
        return sb.ToString();
    }

    public static string DumpFrames(this Exception exception)
    {
        var sb = new StringBuilder();
        var st = new StackTrace(exception, true);
        var frames = st.GetFrames();
        foreach (var frame in frames)
        {
            if (frame != null)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                sb.Append($"File: {frame.GetFileName()}")
                  .Append($", Method: {frame.GetMethod()?.Name}")
                  .Append($", LineNumber: {frame.GetFileLineNumber()}")
                  .Append($"{Environment.NewLine}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// var collection = new[] { 10, 20, 30 };
    /// collection.ForEach(Debug.WriteLine);
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
    {
        foreach (var i in ie)
        {
            try
            {
                action(i);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ForEach: {ex.Message}");
            }
        }
    }

    public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
    {
        var joined = new[] { list1, list2 }.Where(x => x != null).SelectMany(x => x);
        return joined ?? Enumerable.Empty<T>();
    }

    public static IEnumerable<T> JoinLists<T>(this IEnumerable<T> list1, IEnumerable<T> list2, IEnumerable<T> list3)
    {
        var joined = new[] { list1, list2, list3 }.Where(x => x != null).SelectMany(x => x);
        return joined ?? Enumerable.Empty<T>();
    }

    public static IEnumerable<T> JoinMany<T>(params IEnumerable<T>[] array)
    {
        var final = array.Where(x => x != null).SelectMany(x => x);
        return final ?? Enumerable.Empty<T>();
    }

    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        if (target == null) { throw new ArgumentNullException(nameof(target)); }
        if (source == null) { throw new ArgumentNullException(nameof(source)); }
        foreach (var element in source) { target.Add(element); }
    }

    /// <summary>
    /// Gets a string value from a <see cref="StorageFile"/> located in the application local folder.
    /// </summary>
    /// <param name="fileName">
    /// The relative <see cref="string"/> file path.
    /// </param>
    /// <returns>
    /// The stored <see cref="string"/> value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Exception thrown if the <paramref name="fileName"/> is null or empty.
    /// </exception>
    public static async Task<string> ReadLocalFileAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (App.IsPackaged)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync(fileName);
            return await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        }
        else
        {
            using (TextReader reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, fileName)))
            {
                return await reader.ReadToEndAsync(); // uses UTF8 by default
            }
        }
    }


    /// <summary>
    /// IEnumerable file reader.
    /// </summary>
    public static IEnumerable<string> ReadFileLines(string path)
    {
        string? line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// IAsyncEnumerable file reader.
    /// </summary>
    public static async IAsyncEnumerable<string> ReadFileLinesAsync(string path)
    {
        string? line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// File writer for <see cref="IEnumerable{T}"/> parameters.
    /// </summary>
    public static bool WriteFileLines(string path, IEnumerable<string> lines)
    {
        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        return true;
    }

    /// <summary>
    /// De-dupe file reader using a <see cref="HashSet{T}"/>.
    /// </summary>
    public static HashSet<string> ReadLines(string path)
    {
        if (!File.Exists(path))
            return new();

        return new HashSet<string>(File.ReadAllLines(path), StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// De-dupe file writer using a <see cref="HashSet{T}"/>.
    /// </summary>
    public static bool WriteLines(string path, IEnumerable<string> lines)
    {
        var output = new HashSet<string>(lines, StringComparer.InvariantCultureIgnoreCase);

        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in output)
            {
                writer.WriteLine(line);
            }
        }
        return true;
    }


    public static T? DeserializeFromFile<T>(string filePath, ref string error)
    {
        try
        {
            string jsonString = System.IO.File.ReadAllText(filePath);
            T? result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
            error = string.Empty;
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {nameof(DeserializeFromFile)}: {ex.Message}");
            error = ex.Message;
            return default;
        }
    }

    public static bool SerializeToFile<T>(T obj, string filePath, ref string error)
    {
        if (obj == null || string.IsNullOrEmpty(filePath))
            return false;

        try
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(obj);
            System.IO.File.WriteAllText(filePath, jsonString);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {nameof(SerializeToFile)}: {ex.Message}");
            error = ex.Message;
            return false;
        }
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue CastTo<TValue>(TValue value) where TValue : unmanaged
    {
        return (TValue)(object)value;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue? CastToNullable<TValue>(TValue? value) where TValue : unmanaged
    {
        if (value is null)
            return null;

        TValue validValue = value.GetValueOrDefault();
        return (TValue)(object)validValue;
    }
}
