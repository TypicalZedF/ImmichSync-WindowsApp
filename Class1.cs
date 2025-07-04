using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Cryptography;

namespace ImmichSyncApp
{
    internal class Class1
    {
    }

    public class AppSettings
    {
        public string LocalServerUrl { get; set; } = string.Empty;
        public string RemoteServerUrl { get; set; } = string.Empty;
        public string CurrentNetworkId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string JwtToken { get; set; } = string.Empty;
        public List<string> SyncFolders { get; set; } = new();
        public bool PutEachFileInOwnAlbum { get; set; } = false;
        public bool StartMinimizedAtBoot { get; set; } = false;
        public bool CloseButtonMinimizes { get; set; } = false;
        public int UploadSyncDelayMs { get; set; } = 1000;
        public int MaxConcurrentUploads { get; set; } = 3;
        public bool IsQueuePaused { get; set; } = false; // New property to persist queue pause state
    }

    public class SyncState
    {
        public HashSet<string> SyncedFiles { get; set; } = new();
    }

    public enum WorkUnitType
    {
        Upload,
        Download,
        Delete,
        Rename,
        Move
    }

    public enum WorkUnitStatus
    {
        Queued,
        InProgress,
        Completed,
        Failed,
        Paused,
        Cancelled // Add Cancelled as a valid status
    }

    public class WorkUnit
    {
        public WorkUnitType TaskType { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public WorkUnitStatus Status { get; set; } = WorkUnitStatus.Queued;
        public int Priority { get; set; } = 0;
        public int RetryCount { get; set; } = 0;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public WorkUnit(WorkUnitType taskType, string filePath, int priority = 0)
        {
            TaskType = taskType;
            FilePath = filePath;
            Priority = priority;
            Status = WorkUnitStatus.Queued;
        }
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImmichSyncApp", "settings.json");
        private static readonly string SyncStateFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImmichSyncApp", "syncstate.json");

        public static AppSettings Load(Action<string>? onError = null)
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings == null)
                    {
                        onError?.Invoke($"settings.json is invalid or corrupt. Default settings have been restored and a new settings file was created.");
                        var def = new AppSettings();
                        Save(def);
                        return def;
                    }
                    // Decrypt API key if present
                    if (!string.IsNullOrEmpty(settings.JwtToken))
                    {
                        try
                        {
                            var encrypted = Convert.FromBase64String(settings.JwtToken);
                            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                            settings.JwtToken = Encoding.UTF8.GetString(decrypted);
                        }
                        catch { settings.JwtToken = string.Empty; }
                    }
                    return settings;
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"settings.json could not be loaded or is corrupt. Default settings have been restored and a new settings file was created.\nDetails: {ex.Message}");
                var def = new AppSettings();
                Save(def);
                return def;
            }
            var defNew = new AppSettings();
            Save(defNew);
            return defNew;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                // Encrypt API key before saving
                var toSave = new AppSettings
                {
                    LocalServerUrl = settings.LocalServerUrl,
                    RemoteServerUrl = settings.RemoteServerUrl,
                    CurrentNetworkId = settings.CurrentNetworkId,
                    Username = settings.Username,
                    Password = settings.Password,
                    SyncFolders = settings.SyncFolders,
                    PutEachFileInOwnAlbum = settings.PutEachFileInOwnAlbum,
                    JwtToken = settings.JwtToken,
                    StartMinimizedAtBoot = settings.StartMinimizedAtBoot,
                    CloseButtonMinimizes = settings.CloseButtonMinimizes,
                    UploadSyncDelayMs = settings.UploadSyncDelayMs,
                    MaxConcurrentUploads = settings.MaxConcurrentUploads,
                    IsQueuePaused = settings.IsQueuePaused // Save the new property
                };
                if (!string.IsNullOrEmpty(settings.JwtToken))
                {
                    try
                    {
                        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(settings.JwtToken), null, DataProtectionScope.CurrentUser);
                        toSave.JwtToken = Convert.ToBase64String(encrypted);
                    }
                    catch { toSave.JwtToken = string.Empty; }
                }
                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        public static SyncState LoadSyncState(Action<string>? onError = null)
        {
            try
            {
                if (File.Exists(SyncStateFile))
                {
                    var json = File.ReadAllText(SyncStateFile);
                    var state = JsonSerializer.Deserialize<SyncState>(json);
                    if (state == null)
                    {
                        onError?.Invoke($"syncstate.json is invalid or corrupt. Sync state has been reset and a new file was created.");
                        var def = new SyncState();
                        SaveSyncState(def);
                        return def;
                    }
                    return state;
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"syncstate.json could not be loaded or is corrupt. Sync state has been reset and a new file was created.\nDetails: {ex.Message}");
                var def = new SyncState();
                SaveSyncState(def);
                return def;
            }
            var defNew = new SyncState();
            SaveSyncState(defNew);
            return defNew;
        }

        public static void SaveSyncState(SyncState state)
        {
            try
            {
                var dir = Path.GetDirectoryName(SyncStateFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SyncStateFile, json);
            }
            catch { }
        }
    }

    public class QueueManager
    {
        private readonly object _lock = new();
        private readonly List<WorkUnit> _queue = new();
        private int _nextPriority = 1;

        public void Enqueue(WorkUnit workUnit)
        {
            lock (_lock)
            {
                // If restoring from disk, keep original priority; otherwise, assign new
                if (workUnit.Priority == 0)
                {
                    workUnit.Priority = _nextPriority++;
                }
                else if (workUnit.Priority >= _nextPriority)
                {
                    _nextPriority = workUnit.Priority + 1;
                }
                // Prevent duplicate file paths in queue
                if (!_queue.Any(wu => wu.FilePath == workUnit.FilePath && wu.Status == WorkUnitStatus.Queued))
                {
                    _queue.Add(workUnit);
                }
            }
        }

        public WorkUnit? Dequeue()
        {
            lock (_lock)
            {
                if (_queue.Count == 0) return null;
                // Always dequeue the lowest priority (oldest) queued item
                var next = _queue
                    .Where(wu => wu.Status == WorkUnitStatus.Queued)
                    .OrderBy(wu => wu.Priority)
                    .FirstOrDefault();
                if (next != null)
                {
                    _queue.Remove(next);
                    return next;
                }
                return null;
            }
        }

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return !_queue.Any(wu => wu.Status == WorkUnitStatus.Queued);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }

        public List<WorkUnit> GetAllPending()
        {
            lock (_lock)
            {
                return _queue.ToList();
            }
        }

        public void RemoveTask(string filePath)
        {
            lock (_lock)
            {
                _queue.RemoveAll(wu => wu.FilePath == filePath);
            }
        }
    }
}
