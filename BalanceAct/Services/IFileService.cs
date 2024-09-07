using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace BalanceAct.Services;

public interface IDataService
{
    int DaysUntilBackupReplaced { get; set; }
    T? Read<T>(string folderPath, string fileName);
    void Save<T>(string folderPath, string fileName, T content);
    bool Restore(string folderPath, string fileName);
    void Delete(string folderPath, string fileName);
    bool Backup<T>(string folderPath, string fileName, T content);
}

/// <summary>
/// Basic CRUD for our data model.
/// </summary>
public class DataService : IDataService
{
    /// <summary>
    /// Whether to user Newtonsoft or Microsoft for model serialization and deserialization.
    /// </summary>
    public bool UseNewtonsoft { get; set; } = false;

    /// <summary>
    /// <para>Amount (in days) of when a backup should be made.</para>
    /// <para><i>This value must be a negative number.</i></para>
    /// </summary>
    public int DaysUntilBackupReplaced { get; set; } = -1;

    public DataService() { }

    /// <summary>
    /// Loads data of type <typeparamref name="T"/>.
    /// </summary>
    public T? Read<T>(string folderPath, string fileName)
    {
        try
        {
            var path = Path.Combine(folderPath, fileName);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                if (UseNewtonsoft)
                {
                    return JsonConvert.DeserializeObject<T>(json,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        });
                }
                else
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                    var obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
                    return (T)obj;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Read: {ex.Message}");
            throw new Exception(ex.Message, ex);
        }

        return default;
    }

    /// <summary>
    /// Saves data of type <typeparamref name="T"/>.
    /// </summary>
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

            string fileContent = string.Empty;
            if (UseNewtonsoft)
            {
                // Serialize and save to file.
                fileContent = JsonConvert.SerializeObject(content,
                    Newtonsoft.Json.Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    }
                );
                File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
            }
            else
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                fileContent = System.Text.Json.JsonSerializer.Serialize<T>(content, options);
                File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
            }

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

    /// <summary>
    /// Backup data of type <typeparamref name="T"/>.
    /// </summary>
    public bool Backup<T>(string folderPath, string fileName, T content)
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
            
            string fileContent = string.Empty;
            if (UseNewtonsoft)
            {
                // Serialize and save to file.
                fileContent = JsonConvert.SerializeObject(content,
                    Newtonsoft.Json.Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    }
                );

                File.WriteAllText(Path.Combine(folderPath, $"{fileName}.bak"), fileContent, Encoding.UTF8);
            }
            else
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                fileContent = System.Text.Json.JsonSerializer.Serialize<T>(content, options);
                File.WriteAllText(Path.Combine(folderPath, $"{fileName}.bak"), fileContent, Encoding.UTF8);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Backup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Restores data from backup to working.
    /// </summary>
    public bool Restore(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
            return false;

        if (!File.Exists(Path.Combine(folderPath, $"{fileName}.bak")) || !File.Exists(Path.Combine(folderPath, fileName)))
            return false;

        try
        {
            File.Move(Path.Combine(folderPath, $"{fileName}.bak"), Path.Combine(folderPath, fileName), true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Restore: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes the data file.
    /// </summary>
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
