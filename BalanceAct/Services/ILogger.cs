﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BalanceAct.Services;


#region [Logger Enums]
public enum LogLevel
{
    Off = 0,
    Verbose = 1,
    Debug = 2,
    Info = 3,
    Warning = 4,
    Error = 5,
    Critical = 6
}
#endregion

public interface ILogger
{
    LogLevel logLevel { get; set; }

    #region [Event definitions]
    /// <summary>
    /// An event that fires when a message is logged.
    /// </summary>
    event Action<string, LogLevel> OnLogMessage;

    /// <summary>
    /// An event that fires when an exception is thrown.
    /// </summary>
    event Action<Exception> OnException;
    #endregion

    #region [Method definitions]
    void WriteLine(string message, LogLevel level, [CallerMemberName] string caller = "");
    void WriteLines(List<string> messages, LogLevel level, [CallerMemberName] string caller = "");
    Task WriteLineAsync(string message, LogLevel level, [CallerMemberName] string caller = "");
    Task WriteLinesAsync(List<string> messages, LogLevel level, [CallerMemberName] string caller = "");
    string GetCurrentLogPath();
    #endregion
}

/// <summary>
/// Logger implementation.
/// </summary>
public class FileLogger : ILogger
{
    private readonly string mEmpty = "WinUI";
    private readonly string mLogRoot;
    private readonly string mAppName;
    private object threadLock = new object();
    public event Action<string, LogLevel> OnLogMessage = (message, level) => { };
    public event Action<Exception> OnException = (ex) => { };
    public LogLevel logLevel { get; set; }

    #region [Constructors]
    public FileLogger()
    {
        logLevel = LogLevel.Debug;
        mAppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? mEmpty;
        if (App.IsPackaged)
            mLogRoot = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        else
            mLogRoot = System.AppContext.BaseDirectory; // -or- Directory.GetCurrentDirectory()
    }

    public FileLogger(string logPath, LogLevel level)
    {
        logLevel = level;
        mAppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? mEmpty;
        mLogRoot = logPath;
    }
    #endregion

    #region [Method implementation]
    /// <summary>
    /// Use for writing single line of data (synchronous).
    /// </summary>
    public void WriteLine(string message, LogLevel level, [CallerMemberName] string caller = "")
    {
        if (level < logLevel) { return; }

        lock (threadLock)
        {
            string fullPath = GetCurrentLogPath();

            if (!IsPathTooLong(fullPath))
            {
                var directory = Path.GetDirectoryName(fullPath);
                try { Directory.CreateDirectory(directory ?? mEmpty); }
                catch (Exception) { OnException?.Invoke(new Exception($"Could not create directory '{directory}'")); }

                try
                {
                    using (var fileStream = new StreamWriter(File.OpenWrite(fullPath)))
                    {
                        // Jump to the end of the file before writing (same as append)…
                        fileStream.BaseStream.Seek(0, SeekOrigin.End);
                        // Write the text to the file (adds CRLF natively)…
                        fileStream.WriteLine("[{0}]\t{1}\t{2}\t{3}", DateTime.Now.ToString("hh:mm:ss.fff tt"), level, string.IsNullOrEmpty(caller) ? "N/A" : caller, message);
                    }
                    OnLogMessage?.Invoke(message, level);
                }
                catch (Exception ex) { OnException?.Invoke(ex); }
            }
            else { OnException?.Invoke(new Exception($"Path too long: {fullPath}")); }
        }
    }

    /// <summary>
    /// Use for writing large amounts of data at once (synchronous).
    /// </summary>
    public void WriteLines(List<string> messages, LogLevel level, [CallerMemberName] string caller = "")
    {
        if (level < logLevel || messages.Count == 0) { return; }

        lock (threadLock)
        {
            string fullPath = GetCurrentLogPath();

            var directory = Path.GetDirectoryName(fullPath);
            try { Directory.CreateDirectory(directory ?? mEmpty); }
            catch (Exception) { OnException?.Invoke(new Exception($"Could not create directory '{directory}'")); }

            if (!IsPathTooLong(fullPath))
            {
                try
                {
                    using (var fileStream = new StreamWriter(File.OpenWrite(fullPath)))
                    {
                        // Jump to the end of the file before writing (same as append)…
                        fileStream.BaseStream.Seek(0, SeekOrigin.End);
                        foreach (var message in messages)
                        {
                            // Write the text to the file (adds CRLF natively)…
                            fileStream.WriteLine("[{0}]\t{1}\t{2}\t{3}", DateTime.Now.ToString("hh:mm:ss.fff tt"), level, string.IsNullOrEmpty(caller) ? "N/A" : caller, message);
                        }
                    }
                    OnLogMessage?.Invoke($"wrote {messages.Count} lines", level);
                }
                catch (Exception ex) { OnException?.Invoke(ex); }
            }
            else { OnException?.Invoke(new Exception($"Path too long: {fullPath}")); }
        }
    }

    /// <summary>
    /// Use for writing single line of data (asynchronous).
    /// </summary>
    public async Task WriteLineAsync(string message, LogLevel level, [CallerMemberName] string caller = "")
    {
        if (level < logLevel) { return; }

        string fullPath = GetCurrentLogPath();

        var directory = Path.GetDirectoryName(fullPath);
        try { Directory.CreateDirectory(directory ?? mEmpty); }
        catch (Exception) { OnException?.Invoke(new Exception($"Could not create directory '{directory}'")); }

        if (!IsPathTooLong(fullPath))
        {
            try
            {
                using (var fileStream = new StreamWriter(File.OpenWrite(fullPath)))
                {
                    // Jump to the end of the file before writing (same as append)…
                    fileStream.BaseStream.Seek(0, SeekOrigin.End);
                    // Write the text to the file (adds CRLF natively)…
                    await fileStream.WriteLineAsync(string.Format("[{0}]\t{1}\t{2}\t{3}", DateTime.Now.ToString("hh:mm:ss.fff tt"), level, string.IsNullOrEmpty(caller) ? "N/A" : caller, message));
                }
                OnLogMessage?.Invoke(message, level);
            }
            catch (Exception ex) { OnException?.Invoke(ex); }
        }
        else { OnException?.Invoke(new Exception($"Path too long: {fullPath}")); }
    }

    /// <summary>
    /// Use for writing large amounts of data at once (asynchronous).
    /// </summary>
    public async Task WriteLinesAsync(List<string> messages, LogLevel level, [CallerMemberName] string caller = "")
    {
        if (level < logLevel || messages.Count == 0) { return; }

        string fullPath = GetCurrentLogPath();

        var directory = Path.GetDirectoryName(fullPath);
        try { Directory.CreateDirectory(directory ?? mEmpty); }
        catch (Exception) { OnException?.Invoke(new Exception($"Could not create directory '{directory}'")); }

        if (!IsPathTooLong(fullPath))
        {
            try
            {
                using (var fileStream = new StreamWriter(File.OpenWrite(fullPath)))
                {
                    // Jump to the end of the file before writing (same as append)…
                    fileStream.BaseStream.Seek(0, SeekOrigin.End);
                    foreach (var message in messages)
                    {
                        // Write the text to the file (adds CRLF natively)…
                        await fileStream.WriteLineAsync(string.Format("[{0}]\t{1}\t{2}\t{3}", DateTime.Now.ToString("hh:mm:ss.fff tt"), level, string.IsNullOrEmpty(caller) ? "N/A" : caller, message));
                    }
                }
                OnLogMessage?.Invoke($"wrote {messages.Count} lines", level);
            }
            catch (Exception ex) { OnException?.Invoke(ex); }
        }
        else { OnException?.Invoke(new Exception($"Path too long: {fullPath}")); }
    }


    public static List<string> GetCallerInfo()
    {
        List<string> frames = new List<string>();
        StackTrace stackTrace = new StackTrace();

        var fm1 = stackTrace.GetFrame(1)?.GetMethod();
        var fm2 = stackTrace.GetFrame(2)?.GetMethod();
        var fm3 = stackTrace.GetFrame(3)?.GetMethod();
        frames.Add($"[Method1]: {fm1?.Name}  [Class1]: {fm1?.DeclaringType?.Name}");
        frames.Add($"[Method2]: {fm2?.Name}  [Class2]: {fm2?.DeclaringType?.Name}");
        frames.Add($"[Method3]: {fm3?.Name}  [Class3]: {fm3?.DeclaringType?.Name}");
        return frames;
    }
    #endregion

    #region [Path helpers]
    public string GetCurrentLogPath()
    {
        if (!mLogRoot.EndsWith('\\'))
            return $@"{mLogRoot}Logs\{DateTime.Today.Year}\{DateTime.Today.Month.ToString("00")}-{DateTime.Today.ToString("MMMM")}\{mAppName}_{DateTime.Now.ToString("dd")}.log";
        else
            return $@"{mLogRoot}\Logs\{DateTime.Today.Year}\{DateTime.Today.Month.ToString("00")}-{DateTime.Today.ToString("MMMM")}\{mAppName}_{DateTime.Now.ToString("dd")}.log";
    }

    /// <summary>
    /// Gets usable drive from <see cref="DriveType.Fixed"/> volumes.
    /// </summary>
    public static string GetRoot(bool descendingOrder = true)
    {
        string root = string.Empty;
        try
        {
            if (!descendingOrder)
            {
                var logPaths = DriveInfo.GetDrives().Where(di => (di.DriveType == DriveType.Fixed) && (di.IsReady) && (di.AvailableFreeSpace > 1000000)).Select(di => di.RootDirectory).OrderBy(di => di.FullName);
                root = logPaths.FirstOrDefault()?.FullName ?? "C:\\";
            }
            else
            {
                var logPaths = DriveInfo.GetDrives().Where(di => (di.DriveType == DriveType.Fixed) && (di.IsReady) && (di.AvailableFreeSpace > 1000000)).Select(di => di.RootDirectory).OrderByDescending(di => di.FullName);
                root = logPaths.FirstOrDefault()?.FullName ?? "C:\\";
            }
            return root;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetRoot: {ex.Message}");
            return root;
        }
    }

    /// <summary>
    /// Determines the last <see cref="DriveType.Fixed"/> drive letter on a client machine.
    /// </summary>
    /// <returns>drive letter</returns>
    public static string GetLastFixedDrive()
    {
        char lastLetter = 'C';
        DriveInfo[] drives = DriveInfo.GetDrives();
        foreach (DriveInfo drive in drives)
        {
            if (drive.DriveType == DriveType.Fixed && drive.IsReady && drive.AvailableFreeSpace > 1000000)
            {
                if (drive.Name[0] > lastLetter)
                    lastLetter = drive.Name[0];
            }
        }
        return $"{lastLetter}:";
    }

    static bool IsValidPath(string path)
    {
        if ((File.GetAttributes(path) & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint)
        {
            Debug.WriteLine("'" + path + "' is a reparse point (skipped)");
            return false;
        }
        if (!IsReadable(path))
        {
            Debug.WriteLine("'" + path + "' *ACCESS DENIED* (skipped)");
            return false;
        }
        return true;
    }

    static bool IsReadable(string path)
    {
        try
        {
            var dn = Path.GetDirectoryName(path);
            string[] test = Directory.GetDirectories(dn, "*.*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        return true;
    }

    static bool IsPathTooLong(string path)
    {
        try
        {
            var tmp = Path.GetFullPath(path);

            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return true;
        }
    }

    /// <summary>
    /// Testing method for evaluating total path lengths.
    /// </summary>
    /// <param name="rootPath">the root directory to begin searching</param>
    void CheckForLongPaths(string rootPath)
    {
        try
        {
            foreach (string d in Directory.GetDirectories(rootPath))
            {
                if (IsValidPath(d))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        if (f.Length > 259) { LongPathList.Add(f); }
                    }
                    CheckForLongPaths(d);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"UnauthorizedAccess: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType()}: {ex.Message}");
        }
    }
    List<string> LongPathList { get; set; } = new List<string>();
    #endregion
}
