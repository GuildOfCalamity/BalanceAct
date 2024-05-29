﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanceAct.Services;

public interface IDataService
{
    int DaysUntilBackupReplaced { get; set; }
    T? Read<T>(string folderPath, string fileName);
    void Save<T>(string folderPath, string fileName, T content);
    bool Restore(string folderPath, string fileName);
    void Delete(string folderPath, string fileName);
    bool MakeBackup<T>(string folderPath, string fileName, T content);
}

public class DataService : IDataService
{
    public int DaysUntilBackupReplaced { get; set; } = -1;

    public DataService() { }

    public T? Read<T>(string folderPath, string fileName)
    {
        try
        {
            var path = Path.Combine(folderPath, fileName);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Read: {ex.Message}");
            throw new Exception(ex.Message, ex);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (File.Exists(Path.Combine(folderPath, fileName)))
            {
                // Make sure read-only flag is not set.
                FileAttributes attributes = File.GetAttributes(Path.Combine(folderPath, fileName));
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(Path.Combine(folderPath, fileName), attributes);
                }
            }

            // Serialize and save to file.
            var fileContent = JsonConvert.SerializeObject(content,
                Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                }
            );
            File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);

            #region [Automatic backups]
            if (!File.Exists(Path.Combine(folderPath, $"{fileName}.bak")))
                File.WriteAllText(Path.Combine(folderPath, $"{fileName}.bak"), fileContent, Encoding.UTF8);
            else
            {
                var backDate = DateTime.Now.AddDays(DaysUntilBackupReplaced);
                var fi = new FileInfo(Path.Combine(folderPath, $"{fileName}.bak")).LastWriteTime;
                if (fi <= backDate)
                {
                    File.Delete(Path.Combine(folderPath, $"{fileName}.bak"));
                    File.WriteAllText(Path.Combine(folderPath, $"{fileName}.bak"), fileContent, Encoding.UTF8);
                }
            }
            #endregion
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Save: {ex.Message}");
            throw new Exception(ex.Message, ex);
        }
    }

    public bool MakeBackup<T>(string folderPath, string fileName, T content)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (File.Exists(Path.Combine(folderPath, $"{fileName}.bak")))
            {
                // Make sure read-only flag is not set.
                FileAttributes attributes = File.GetAttributes(Path.Combine(folderPath, $"{fileName}.bak"));
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(Path.Combine(folderPath, $"{fileName}.bak"), attributes);
                }
            }

            // Serialize and save to file.
            var fileContent = JsonConvert.SerializeObject(content,
                Newtonsoft.Json.Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                }
            );

            File.WriteAllText(Path.Combine(folderPath, $"{fileName}.bak"), fileContent, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Backup: {ex.Message}");
            return false;
        }
    }

    public bool Restore(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
            return false;

        if (!File.Exists(Path.Combine(folderPath, $"{fileName}.bak")) || !File.Exists(Path.Combine(folderPath, fileName)))
            return false;

        try
        {
            //File.Delete(Path.Combine(folderPath, fileName));
            File.Move(Path.Combine(folderPath, $"{fileName}.bak"), Path.Combine(folderPath, fileName), true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Restore: {ex.Message}");
            return false;
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(folderPath, fileName)))
            {
                // Make sure read-only flag is not set.
                FileAttributes attributes = File.GetAttributes(Path.Combine(folderPath, fileName));
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(Path.Combine(folderPath, fileName), attributes);
                }

                // Remove the file.
                File.Delete(Path.Combine(folderPath, fileName));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Delete: {ex.Message}");
            throw new Exception(ex.Message, ex);
        }
    }
}
