using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.Win32;

namespace ImmichSyncApp
{
    public partial class Form1 : Form
    {
        private List<string> syncFolders = new();
        private Dictionary<string, FileSystemWatcher> watchers = new();
        private CancellationTokenSource uploadCts = new();
        private int maxConcurrentUploads = 3; // Configurable
        private string localServerUrl = "http://192.168.1.100:2283"!; // Set your local Immich server URL
        private string remoteServerUrl = "https://yourdomain.com"!; // Set your remote Immich server URL
        private string currentNetworkId = string.Empty;
        private string currentServerUrl => GetServerUrlForCurrentNetwork();
        private bool isUploading = false;
        private HttpClient httpClient = new();
        private AppSettings appSettings = null!; // Initialize to null to suppress CS8618
        private SyncState syncState = null!;    // Initialize to null to suppress CS8618
        // Add a dictionary to track file statuses for the queue
        private Dictionary<string, string> fileQueueStatus = new();
        private Dictionary<string, int> uploadFailureCounts = new();
        private HashSet<string> ignoredFiles = new();
        private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImmichSyncApp", "ImmichSyncApp.log");

        // Add this at the top of the class
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".heic", ".mp4", ".mov", ".avi", ".webp", ".gif", ".bmp", ".tiff", ".mkv", ".3gp", ".webm"
        };

        private Dictionary<string, CancellationTokenSource> folderSyncDelays = new();

        // Track created albums by folder name
        private readonly Dictionary<string, string> folderAlbumIds = new();
        // Synchronize album creation per folder
        private readonly ConcurrentDictionary<string, SemaphoreSlim> albumLocks = new();

        // Add a static lock object to LogToFile to prevent concurrent file access exceptions
        private static readonly object LogFileLock = new();

        private QueueManager queueManager = new();

        private static readonly string QueueStateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImmichSyncApp", "queue.json");

        // Add a flag to indicate if the queue was paused due to an error
        private bool pauseDueToError = false;

        // Track the current batch of work units being processed
        private List<WorkUnit> currentBatch = new();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            // Add tray menu items and handlers (moved from Designer file)
            trayMenu.Items.Add("Show", null, (s, e) =>
            {
                this.ShowInTaskbar = true;
                this.WindowState = FormWindowState.Normal;
                this.Show();
                this.Activate();
                this.BringToFront();
            });
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            // Wire up the new checkbox event
            chkStartMinimizedAtBoot.CheckedChanged += chkStartMinimizedAtBoot_CheckedChanged;
            chkCloseButtonMinimizes.CheckedChanged += chkCloseButtonMinimizes_CheckedChanged;
            // Set the main window icon to the application icon (from the .exe)
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            // Set the tray icon to the same application icon
            notifyIcon1.Icon = this.Icon;
            // Handle FormClosing to minimize to tray
            this.FormClosing += Form1_FormClosing;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        // Helper to minimize to tray
        private void MinimizeToTray()
        {
            this.Hide();
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }

        private void Form1_Load(object? sender, EventArgs? e)
        {
            // Load settings with error popups
            appSettings = SettingsManager.Load(msg => MessageBox.Show(msg, "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            syncState = SettingsManager.LoadSyncState(msg => MessageBox.Show(msg, "Sync State Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            // Set maxConcurrentUploads from settings
            maxConcurrentUploads = appSettings.MaxConcurrentUploads > 0 ? appSettings.MaxConcurrentUploads : 3;
            localServerUrl = appSettings.LocalServerUrl;
            remoteServerUrl = appSettings.RemoteServerUrl;
            currentNetworkId = appSettings.CurrentNetworkId;
            cmbLocalServerUrl.Text = localServerUrl;
            cmbRemoteServerUrl.Text = remoteServerUrl;
            txtApiKey.Text = appSettings.JwtToken;
            UpdateCurrentNetworkLabel();
            // Restore watched folders
            syncFolders.Clear();
            lstFolders.Items.Clear();
            foreach (var folder in appSettings.SyncFolders.ToList())
            {
                if (!syncFolders.Contains(folder) && Directory.Exists(folder))
                {
                    syncFolders.Add(folder);
                    lstFolders.Items.Add(folder);
                    AddWatcher(folder);
                }
            }
            // Scan for unsynced files and queue them
            foreach (var folder in syncFolders)
            {
                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        if ((syncState.SyncedFiles == null || !syncState.SyncedFiles.Contains(file)) &&
                            SupportedExtensions.Contains(Path.GetExtension(file)))
                        {
                            var workUnit = new WorkUnit(WorkUnitType.Upload, file);
                            queueManager.Enqueue(workUnit);
                            UpdateQueueStatus(file, "Queued");
                        }
                    }
                }
            }
            // Restore queue paused state
            if (appSettings.IsQueuePaused)
            {
                // Mark all queued items as Paused in the UI
                foreach (DataGridViewRow row in dgvQueue.Rows)
                {
                    var status = row.Cells[1].Value as string;
                    if (status == "Queued" || status == "Syncing")
                    {
                        row.Cells[1].Value = "Paused";
                        fileQueueStatus[(string)row.Cells[0].Value] = "Paused";
                    }
                }
                btnPauseResumeQueue.Text = "Resume Queue";
                isUploading = false;
            }
            else
            {
                StartUploadWorker();
            }
            // Wire up change events for unsaved status
            cmbLocalServerUrl.TextChanged += (s, e) => lblServerSettingsStatus.Text = "Unsaved changes";
            cmbRemoteServerUrl.TextChanged += (s, e) => lblServerSettingsStatus.Text = "Unsaved changes";
            txtApiKey.TextChanged += (s, e) => lblServerSettingsStatus.Text = "Unsaved changes";
            btnSetCurrentNetwork.Click += (s, e) => lblServerSettingsStatus.Text = "Unsaved changes";

            // Remove LastSyncedItems UI restore logic

            // Set the checkbox state from settings
            chkCreateAlbumsForSyncedFolders.CheckedChanged -= chkCreateAlbumsForSyncedFolders_CheckedChanged;
            chkCreateAlbumsForSyncedFolders.Checked = appSettings.PutEachFileInOwnAlbum;
            chkCreateAlbumsForSyncedFolders.CheckedChanged += chkCreateAlbumsForSyncedFolders_CheckedChanged;
            // Set the new checkbox state from settings
            chkStartMinimizedAtBoot.CheckedChanged -= chkStartMinimizedAtBoot_CheckedChanged;
            chkStartMinimizedAtBoot.Checked = appSettings.StartMinimizedAtBoot;
            chkStartMinimizedAtBoot.CheckedChanged += chkStartMinimizedAtBoot_CheckedChanged;
            // Set the close button minimize checkbox from settings
            chkCloseButtonMinimizes.CheckedChanged -= chkCloseButtonMinimizes_CheckedChanged;
            chkCloseButtonMinimizes.Checked = appSettings.CloseButtonMinimizes;
            chkCloseButtonMinimizes.CheckedChanged += chkCloseButtonMinimizes_CheckedChanged;
            // If started at boot and setting is enabled, minimize
            if (appSettings.StartMinimizedAtBoot && Environment.GetCommandLineArgs().Any(arg => arg.Contains("-minimized")))
            {
                WindowState = FormWindowState.Minimized;
                MinimizeToTray();
            }

            ValidateSyncFolders();
            LoadQueueState();
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog1.SelectedPath;
                if (!syncFolders.Contains(selectedPath))
                {
                    syncFolders.Add(selectedPath);
                    lstFolders.Items.Add(selectedPath);
                    AddWatcher(selectedPath);
                    appSettings.SyncFolders = syncFolders.ToList();
                    SettingsManager.Save(appSettings); // Save folders immediately
                    if (chkCreateAlbumsForSyncedFolders.Checked)
                    {
                        _ = EnsureAlbumsForAllFoldersAsync(CancellationToken.None);
                    }
                    // Start a 10-second delay before scanning and queuing files
                    var cts = new CancellationTokenSource();
                    folderSyncDelays[selectedPath] = cts;
                    Task.Run(async () =>
                    {
                        try { await Task.Delay(10000, cts.Token); } catch { return; }
                        if (!syncFolders.Contains(selectedPath)) return; // Folder was removed
                        BeginInvoke(() => ScanAndQueueFolder(selectedPath));
                        folderSyncDelays.Remove(selectedPath);
                    });
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem is string selectedPath && syncFolders.Contains(selectedPath))
            {
                syncFolders.Remove(selectedPath);
                lstFolders.Items.Remove(selectedPath);
                if (watchers.TryGetValue(selectedPath, out var watcher))
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watchers.Remove(selectedPath);
                }
                // Cancel any pending sync delay
                if (folderSyncDelays.TryGetValue(selectedPath, out var cts))
                {
                    cts.Cancel();
                    folderSyncDelays.Remove(selectedPath);
                }
                appSettings.SyncFolders = syncFolders.ToList();
                SettingsManager.Save(appSettings);
            }
        }

        private void ScanAndQueueFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    if ((syncState.SyncedFiles == null || !syncState.SyncedFiles.Contains(file)) &&
                        SupportedExtensions.Contains(Path.GetExtension(file)))
                    {
                        var workUnit = new WorkUnit(WorkUnitType.Upload, file);
                        queueManager.Enqueue(workUnit);
                        UpdateQueueStatus(file, "Queued");
                    }
                }
            }
            StartUploadWorker();
        }

        private void AddWatcher(string folderPath)
        {
            var watcher = new FileSystemWatcher(folderPath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            watcher.Created += OnFileCreated;
            watchers[folderPath] = watcher;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            // Only queue if not already synced and is a supported file type
            if ((syncState.SyncedFiles == null || !syncState.SyncedFiles.Contains(e.FullPath)) &&
                SupportedExtensions.Contains(Path.GetExtension(e.FullPath)))
            {
                var workUnit = new WorkUnit(WorkUnitType.Upload, e.FullPath);
                queueManager.Enqueue(workUnit);
                BeginInvoke(() => UpdateQueueStatus(e.FullPath, "Queued"));
                StartUploadWorker();
            }
        }

        private void UpdateQueueStatus(string filePath, string status, DateTime? timestamp = null)
        {
            fileQueueStatus[filePath] = status;
            // Find row or add new
            var row = dgvQueue.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (string)r.Cells[0].Value == filePath);
            // Use 12-hour format with AM/PM for timestamp
            string ts = timestamp?.ToString("yyyy-MM-dd hh:mm:ss tt") ?? string.Empty;
            if (row == null)
            {
                // Insert new queued items at the top
                dgvQueue.Rows.Insert(0, filePath, status, ts);
            }
            else
            {
                row.Cells[1].Value = status;
                row.Cells[2].Value = ts;
            }
        }

        private void StartUploadWorker()
        {
            if (isUploading) return;
            isUploading = true;
            Task.Run(async () => await ProcessQueueAsync(uploadCts.Token));
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Gather up to maxConcurrentUploads work units for this batch
                var batch = new List<WorkUnit>();
                for (int i = 0; i < maxConcurrentUploads; i++)
                {
                    var workUnit = queueManager.Dequeue();
                    if (workUnit != null)
                        batch.Add(workUnit);
                    else
                        break;
                }

                // Track the current batch for pause/resume
                currentBatch = batch;

                if (batch.Count == 0)
                {
                    isUploading = false;
                    currentBatch = new List<WorkUnit>();
                    return;
                }

                // Wait for the configured delay before starting the batch
                int delayMs = appSettings.UploadSyncDelayMs > 0 ? appSettings.UploadSyncDelayMs : 1000;
                await Task.Delay(delayMs, token);

                // Start all uploads in the batch concurrently
                var tasks = batch.Select(wu => HandleWorkUnitAsync(wu, token)).ToList();
                await Task.WhenAll(tasks);
            }
            // Clear currentBatch when done
            currentBatch = new List<WorkUnit>();
        }

        private async Task HandleWorkUnitAsync(WorkUnit workUnit, CancellationToken token)
        {
            try
            {
                switch (workUnit.TaskType)
                {
                    case WorkUnitType.Upload:
                        await UploadFileAsync(workUnit.FilePath, token);
                        workUnit.Status = WorkUnitStatus.Completed;
                        break;
                        // Add cases for other WorkUnitType values as needed
                }
            }
            catch (Exception ex)
            {
                workUnit.RetryCount++;
                if (workUnit.RetryCount < 5)
                {
                    queueManager.Enqueue(workUnit);
                }
                else
                {
                    workUnit.Status = WorkUnitStatus.Failed;
                    LogToFile($"Work unit failed after 5 retries: {workUnit.FilePath}. Error: {ex.Message}");
                }
            }
        }

        private async Task<bool> EnsureAuthenticatedAsync()
        {
            if (!string.IsNullOrEmpty(appSettings.JwtToken))
                return true;
            await Task.Yield(); // avoid warning
            // No popup, just return false
            return false;
        }

        private async Task<bool> EnsureAlbumExistsAsync(string albumName, CancellationToken token)
        {
            string albumUrl = $"{currentServerUrl}/api/albums";
            try
            {
                // Add API key header to GET request
                var getRequest = new HttpRequestMessage(HttpMethod.Get, albumUrl);
                getRequest.Headers.Add("x-api-key", appSettings.JwtToken);
                var response = await httpClient.SendAsync(getRequest, token);
                response.EnsureSuccessStatusCode();
                var albums = await ReadAlbumsAsync(response.Content);
                if (albums.Any(a => a.name == albumName))
                {
                    return true;
                }

                var createAlbumRequest = new HttpRequestMessage(HttpMethod.Post, albumUrl)
                {
                    Content = new StringContent($"{{ \"name\": \"{albumName}\" }}", Encoding.UTF8, "application/json")
                };
                createAlbumRequest.Headers.Add("x-api-key", appSettings.JwtToken);
                var createResponse = await httpClient.SendAsync(createAlbumRequest, token);
                if (createResponse.IsSuccessStatusCode)
                {
                    LogToFile($"SUCCESS: Album '{albumName}' created successfully.");
                    return true;
                }
                // If album already exists or bad request, treat as success
                if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict || createResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    LogToFile($"INFO: Album '{albumName}' already exists or bad request (HTTP {(int)createResponse.StatusCode}).");
                    return true;
                }
                // Otherwise, throw
                createResponse.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict || ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                LogToFile($"INFO: Album '{albumName}' already exists or bad request (HTTP {(int)ex.StatusCode}).");
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"FAILURE: Failed to ensure album '{albumName}' exists. Error: {ex.Message}");
                MessageBox.Show($"Failed to create album '{albumName}'.\nError: {ex.Message}", "Album Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<string?> EnsureAlbumForFolderAsync(string folder, CancellationToken token)
        {
            var folderName = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(folderName)) return null;
            if (folderAlbumIds.TryGetValue(folder, out var existingId)) return existingId;
            var sem = albumLocks.GetOrAdd(folder, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(token);
            try
            {
                // Double-check after acquiring lock
                if (folderAlbumIds.TryGetValue(folder, out existingId)) return existingId;
                // Get all albums
                var req = new HttpRequestMessage(HttpMethod.Get, $"{currentServerUrl}/api/albums");
                req.Headers.Add("x-api-key", appSettings.JwtToken);
                var resp = await httpClient.SendAsync(req, token);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync(token);
                var albums = System.Text.Json.JsonDocument.Parse(json).RootElement;
                foreach (var album in albums.EnumerateArray())
                {
                    if (album.TryGetProperty("albumName", out var nameProp) && nameProp.GetString() == folderName)
                    {
                        var id = album.GetProperty("id").GetString();
                        folderAlbumIds[folder] = id!;
                        return id;
                    }
                }
                // Not found, create it
                var createReq = new HttpRequestMessage(HttpMethod.Post, $"{currentServerUrl}/api/albums");
                createReq.Headers.Add("x-api-key", appSettings.JwtToken);
                var body = $"{{\"albumName\":\"{folderName}\"}}";
                createReq.Content = new StringContent(body, Encoding.UTF8, "application/json");
                var createResp = await httpClient.SendAsync(createReq, token);
                createResp.EnsureSuccessStatusCode();
                var createJsonString = await createResp.Content.ReadAsStringAsync(token);
                var createJson = System.Text.Json.JsonDocument.Parse(createJsonString).RootElement;
                var newId = createJson.GetProperty("id").GetString();
                folderAlbumIds[folder] = newId!;
                LogToFile($"Album '{folderName}' created with id {newId}.");
                return newId;
            }
            catch (Exception ex)
            {
                LogToFile($"Error ensuring album for folder '{folder}': {ex.Message}");
                // Only show the popup if this is not a TaskCanceledException (to avoid double popups)
                if (ex is not TaskCanceledException && ex.InnerException is not TaskCanceledException)
                {
                    MessageBox.Show($"Failed to create album for folder '{folderName}'.\nError: {ex.Message}", "Album Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        private async Task AddAssetToAlbumAsync(string albumId, string assetId, CancellationToken token)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Put, $"{currentServerUrl}/api/albums/{albumId}/assets");
                req.Headers.Add("x-api-key", appSettings.JwtToken);
                var body = $"{{\"ids\":[\"{assetId}\"]}}";
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await httpClient.SendAsync(req, token);
                if (resp.IsSuccessStatusCode)
                {
                    LogToFile($"Asset {assetId} added to album {albumId}.");
                    return;
                }
                // If 400 Bad Request, album may have been deleted. Try to recreate and retry once.
                if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Find the folder for this albumId
                    var folder = folderAlbumIds.FirstOrDefault(x => x.Value == albumId).Key;
                    if (!string.IsNullOrEmpty(folder))
                    {
                        folderAlbumIds.Remove(folder);
                        LogToFile($"Album {albumId} for folder '{folder}' may have been deleted. Recreating and retrying...");
                        var newAlbumId = await EnsureAlbumForFolderAsync(folder, token);
                        if (!string.IsNullOrEmpty(newAlbumId))
                        {
                            // Retry once
                            req = new HttpRequestMessage(HttpMethod.Put, $"{currentServerUrl}/api/albums/{newAlbumId}/assets");
                            req.Headers.Add("x-api-key", appSettings.JwtToken);
                            body = $"{{\"ids\":[\"{assetId}\"]}}";
                            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                            resp = await httpClient.SendAsync(req, token);
                            if (resp.IsSuccessStatusCode)
                            {
                                LogToFile($"Asset {assetId} added to recreated album {newAlbumId}.");
                                return;
                            }
                        }
                    }
                }
                LogToFile($"Error adding asset {assetId} to album {albumId}: Response status code does not indicate success: {(int)resp.StatusCode} ({resp.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogToFile($"Error adding asset {assetId} to album {albumId}: {ex.Message}");
            }
        }

        private async Task UploadFileAsync(string filePath, CancellationToken token)
        {
            if (ignoredFiles.Contains(filePath))
                return;

            // Wait for the configured delay BEFORE uploading
            int delayMs = appSettings.UploadSyncDelayMs > 0 ? appSettings.UploadSyncDelayMs : 1000;
            await Task.Delay(delayMs, token);

            // Set status to Syncing before upload
            BeginInvoke(() => UpdateQueueStatus(filePath, "Syncing"));

            while (!await CanConnectToServerAsync())
            {
                await Task.Delay(5000, token);
            }

            if (!await EnsureAuthenticatedAsync())
            {
                BeginInvoke(() => UpdateQueueStatus(filePath, "Failed"));
                return;
            }

            string uploadUrl = $"{currentServerUrl}/api/assets";
            string debugInfo = $"Upload Request URL: {uploadUrl}\r\n";

            try
            {
                // If album creation is enabled, ensure album exists for this folder
                string? albumId = null;
                if (appSettings.PutEachFileInOwnAlbum)
                {
                    var folder = syncFolders.FirstOrDefault(f => filePath.StartsWith(f, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(folder))
                    {
                        albumId = await EnsureAlbumForFolderAsync(folder, token);
                        // If album creation failed (e.g. due to permissions), mark as Failed and pause queue
                        if (albumId == null)
                        {
                            BeginInvoke(() => UpdateQueueStatus(filePath, "Failed"));
                            LogToFile($"FAILURE: {filePath} failed to upload because album could not be created (permissions or other error). Queue paused.");
                            BeginInvoke(() => PauseQueueProgrammatically());
                            return;
                        }
                    }
                }

                using var content = new MultipartFormDataContent();
                var fileInfo = new FileInfo(filePath);
                var deviceAssetId = Guid.NewGuid().ToString();
                var deviceId = Environment.MachineName;
                var fileCreatedAt = fileInfo.CreationTimeUtc.ToString("o");
                var fileModifiedAt = fileInfo.LastWriteTimeUtc.ToString("o");
                var filename = Path.GetFileName(filePath);

                content.Add(new StreamContent(File.OpenRead(filePath)), "assetData", filename);
                content.Add(new StringContent(deviceAssetId), "deviceAssetId");
                content.Add(new StringContent(deviceId), "deviceId");
                content.Add(new StringContent(fileCreatedAt), "fileCreatedAt");
                content.Add(new StringContent(fileModifiedAt), "fileModifiedAt");
                content.Add(new StringContent(filename), "filename");

                var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(appSettings.JwtToken))
                {
                    request.Headers.Add("x-api-key", appSettings.JwtToken);
                    debugInfo += "x-api-key: (provided)\r\n";
                }
                else
                {
                    debugInfo += "x-api-key: (none)\r\n";
                }

                var response = await httpClient.SendAsync(request, token);
                debugInfo += $"Status Code: {(int)response.StatusCode} {response.StatusCode}\r\n";
                string responseBody = await response.Content.ReadAsStringAsync();
                debugInfo += $"Response Body:\r\n{responseBody}\r\n";
                response.EnsureSuccessStatusCode();

                // Parse assetId from response
                string? assetId = null;
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(responseBody).RootElement;
                    assetId = json.GetProperty("id").GetString();
                }
                catch { }

                var now = DateTime.Now;
                BeginInvoke(() => UpdateQueueStatus(filePath, "Synced", now));

                if (syncState.SyncedFiles == null) syncState.SyncedFiles = new HashSet<string>();
                syncState.SyncedFiles.Add(filePath);

                SettingsManager.SaveSyncState(syncState);
                uploadFailureCounts.Remove(filePath);

                LogToFile($"SUCCESS: {filePath} uploaded successfully.");

                // Add asset to album if enabled and assetId/albumId are available
                if (appSettings.PutEachFileInOwnAlbum && !string.IsNullOrEmpty(albumId) && !string.IsNullOrEmpty(assetId))
                {
                    await AddAssetToAlbumAsync(albumId, assetId, token);
                }
            }
            catch (OperationCanceledException)
            {
                // If the queue was paused due to an error, mark as Failed, else Paused
                if (pauseDueToError)
                {
                    BeginInvoke(() => UpdateQueueStatus(filePath, "Failed"));
                }
                else
                {
                    BeginInvoke(() => UpdateQueueStatus(filePath, "Paused"));
                }
            }
            catch (Exception ex)
            {
                int failCount = uploadFailureCounts.GetValueOrDefault(filePath, 0) + 1;
                uploadFailureCounts[filePath] = failCount;

                LogToFile($"FAILURE: {filePath} failed to upload. Attempt {failCount}. Error: {ex.Message}\n{debugInfo}");
                BeginInvoke(() => UpdateQueueStatus(filePath, "Failed"));

                // No popup, just mark as Failed after 5 attempts
                if (failCount < 5)
                {
                    await Task.Delay(30000, token);
                    var workUnit = new WorkUnit(WorkUnitType.Upload, filePath);
                    queueManager.Enqueue(workUnit);
                }
                // else: do nothing, status remains Failed
            }
            finally
            {
                // Reset the error flag after all tasks have been processed
                if (pauseDueToError && !isUploading)
                {
                    pauseDueToError = false;
                }
            }
        }

        // Checkbox for album creation (rename for clarity)
        private void chkCreateAlbumsForSyncedFolders_CheckedChanged(object sender, EventArgs e)
        {
            if (appSettings == null) return;
            appSettings.PutEachFileInOwnAlbum = chkCreateAlbumsForSyncedFolders.Checked;
            SettingsManager.Save(appSettings);
            if (chkCreateAlbumsForSyncedFolders.Checked)
            {
                folderAlbumIds.Clear();
                albumLocks.Clear();
                _ = EnsureAlbumsForAllFoldersAsync(CancellationToken.None);
            }
        }

        // Only create albums for folders, not during upload
        private async Task EnsureAlbumsForAllFoldersAsync(CancellationToken token)
        {
            foreach (var folder in syncFolders)
            {
                var folderName = Path.GetFileName(folder);
                if (!string.IsNullOrEmpty(folderName))
                {
                    await EnsureAlbumExistsAsync(folderName, token);
                }
            }
        }

        // Restore missing helper/event methods for build
        private string GetServerUrlForCurrentNetwork()
        {
            // Dummy implementation for build
            return localServerUrl;
        }
        private void UpdateCurrentNetworkLabel()
        {
            if (string.IsNullOrEmpty(currentNetworkId))
            {
                lblCurrentNetwork.Text = "Current local network: (not set)";
            }
            else
            {
                lblCurrentNetwork.Text = $"Current local network: {currentNetworkId}";
            }
        }
        private async Task TestConnectionAsync()
        {
            string localUrl = cmbLocalServerUrl.Text.Trim();
            string remoteUrl = cmbRemoteServerUrl.Text.Trim();
            string apiKey = txtApiKey.Text.Trim();
            StringBuilder debug = new();
            var results = new List<string>();

            if (string.IsNullOrEmpty(localUrl) && string.IsNullOrEmpty(remoteUrl))
            {
                MessageBox.Show("Please enter at least one server URL.", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter the API key.", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var timeout = TimeSpan.FromSeconds(10);
            foreach (var (url, label) in new[] { (localUrl, "Local"), (remoteUrl, "Remote") })
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                debug.AppendLine($"==== {label} Server: {url} ====");
                bool pingOk = false, canReadAlbums = false, canCreateAlbum = false, canUpload = false;
                string pingStatus = "", albumsStatus = "", uploadStatus = "";
                string readAlbumsDebug = "", createAlbumDebug = "";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(1);
                // 1. Ping
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, url + "/server-info/ping");
                    req.Headers.Add("x-api-key", apiKey);
                    var resp = await client.SendAsync(req);
                    pingOk = resp.IsSuccessStatusCode;
                    pingStatus = pingOk ? "PASS" : $"FAIL ({(int)resp.StatusCode} {resp.StatusCode})";
                    debug.AppendLine($"Ping: {url}/server-info/ping => {pingStatus}");
                }
                catch (Exception ex)
                {
                    pingStatus = $"FAIL ({ex.Message})";
                    debug.AppendLine($"Ping: {url}/server-info/ping => {pingStatus}");
                }
                // 2. Albums (Read + Create)
                // 2a. Read
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, url + "/api/albums");
                    req.Headers.Add("x-api-key", apiKey);
                    var resp = await client.SendAsync(req);
                    canReadAlbums = resp.IsSuccessStatusCode;
                    readAlbumsDebug = canReadAlbums ? "PASS" : $"FAIL ({(int)resp.StatusCode} {resp.StatusCode})";
                    debug.AppendLine($"Albums Read: {url}/api/albums [GET] => {readAlbumsDebug}");
                }
                catch (Exception ex)
                {
                    canReadAlbums = false;
                    readAlbumsDebug = $"FAIL ({ex.Message})";
                    debug.AppendLine($"Albums Read: {url}/api/albums [GET] => {readAlbumsDebug}");
                }
                // 2b. Create
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, url + "/api/albums");
                    req.Headers.Add("x-api-key", apiKey);
                    req.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                    var resp = await client.SendAsync(req);
                    if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest || resp.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        canCreateAlbum = true;
                        createAlbumDebug = (resp.StatusCode == System.Net.HttpStatusCode.BadRequest) ? "PASS (permission, but bad request)" : "PASS (album created)";
                    }
                    else if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden || resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        canCreateAlbum = false;
                        createAlbumDebug = $"FAIL ({(int)resp.StatusCode} {resp.StatusCode})";
                    }
                    else
                    {
                        canCreateAlbum = false;
                        createAlbumDebug = $"FAIL ({(int)resp.StatusCode} {resp.StatusCode})";
                    }
                    debug.AppendLine($"Albums Create: {url}/api/albums [POST empty] => {createAlbumDebug}");
                }
                catch (Exception ex)
                {
                    canCreateAlbum = false;
                    createAlbumDebug = $"FAIL ({ex.Message})";
                    debug.AppendLine($"Albums Create: {url}/api/albums [POST empty] => {createAlbumDebug}");
                }
                // 2c. Combined Albums result
                if (canReadAlbums && canCreateAlbum)
                    albumsStatus = "PASS";
                else if (!canReadAlbums && !canCreateAlbum)
                    albumsStatus = "FAIL";
                else
                    albumsStatus = "PARTIAL";
                // 3. Upload Permission (POST /api/assets, dummy file)
                string dummyFilePath = Path.GetTempFileName();
                try
                {
                    using (var fs = File.OpenWrite(dummyFilePath))
                    {
                        fs.Write(new byte[10], 0, 10); // Write 10 bytes
                    }
                    using var content = new MultipartFormDataContent();
                    content.Add(new StreamContent(File.OpenRead(dummyFilePath)), "assetData", "dummy.jpg");
                    content.Add(new StringContent(Guid.NewGuid().ToString()), "deviceAssetId");
                    content.Add(new StringContent(Environment.MachineName), "deviceId");
                    content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "fileCreatedAt");
                    content.Add(new StringContent(DateTime.UtcNow.ToString("o")), "fileModifiedAt");
                    content.Add(new StringContent("dummy.jpg"), "filename");
                    var req = new HttpRequestMessage(HttpMethod.Post, url + "/api/assets") { Content = content };
                    req.Headers.Add("x-api-key", apiKey);
                    var resp = await client.SendAsync(req);
                    canUpload = resp.IsSuccessStatusCode;
                    uploadStatus = canUpload ? "PASS" : $"FAIL ({(int)resp.StatusCode} {resp.StatusCode})";
                    debug.AppendLine($"Upload: {url}/api/assets [POST] => {uploadStatus}");
                }
                catch (Exception ex)
                {
                    uploadStatus = $"FAIL ({ex.Message})";
                    debug.AppendLine($"Upload: {url}/api/assets [POST] => {uploadStatus}");
                }
                finally
                {
                    try { File.Delete(dummyFilePath); } catch { }
                }
                results.Add($"{label} Server:\n  Ping: {pingStatus}\n  Albums: {albumsStatus}\n  Upload: {uploadStatus}");
                debug.AppendLine($"Albums Combined: {albumsStatus} (Read: {readAlbumsDebug}, Create: {createAlbumDebug})");
            }

            string summary = string.Join("\n\n", results);
            var result = MessageBox.Show(summary + "\n\nShow debug details?", "Test Connection Results", MessageBoxButtons.YesNo, results.All(r => r.Contains("PASS")) ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            if (result == DialogResult.Yes)
            {
                var debugWin = new DebugWindow(debug.ToString());
                debugWin.ShowDialog();
            }
        }

        // Add this handler to fix missing event handler error
        private void chkStartMinimizedAtBoot_CheckedChanged(object sender, EventArgs e)
        {
            if (appSettings == null) return;
            appSettings.StartMinimizedAtBoot = chkStartMinimizedAtBoot.Checked;
            SettingsManager.Save(appSettings);
            SetStartup(appSettings.StartMinimizedAtBoot);
        }

        private void chkCloseButtonMinimizes_CheckedChanged(object? sender, EventArgs? e)
        {
            if (appSettings == null) return;
            appSettings.CloseButtonMinimizes = chkCloseButtonMinimizes.Checked;
            SettingsManager.Save(appSettings);
        }

        // Add handler to minimize to tray on close
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && appSettings.CloseButtonMinimizes)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
            else
            {
                SaveQueueState();
            }
        }

        // Add this helper to fix missing ReadAlbumsAsync error
        private async Task<List<dynamic>> ReadAlbumsAsync(HttpContent content)
        {
            // Dummy implementation for build
            await Task.Yield();
            return new List<dynamic>();
        }

        // Add this helper to fix missing LogToFile error
        private void LogToFile(string message)
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir!);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{Environment.MachineName}] [Thread:{System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}\r\n";
                lock (LogFileLock)
                {
                    File.AppendAllText(LogFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Debug.WriteLine($"LogToFile failed: {ex}"); } catch { }
            }
        }

        // Add this helper to fix missing CanConnectToServerAsync error
        private async Task<bool> CanConnectToServerAsync()
        {
            // Dummy implementation for build
            await Task.Yield();
            return true;
        }

        // Add missing SetStartup method
        private void SetStartup(bool enable)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            string appName = "ImmichSyncApp";
            string exePath = Application.ExecutablePath;
            if (enable)
            {
                // Add -minimized argument
                string value = $"\"{exePath}\" -minimized";
                using (var key = Registry.CurrentUser.OpenSubKey(runKey, true) ?? Registry.CurrentUser.CreateSubKey(runKey))
                {
                    key.SetValue(appName, value);
                }
            }
            else
            {
                using (var key = Registry.CurrentUser.OpenSubKey(runKey, true))
                {
                    key?.DeleteValue(appName, false);
                }
            }
        }

        private void ValidateSyncFolders()
        {
            var invalidFolders = new List<string>();

            foreach (var folder in syncFolders)
            {
                if (!Directory.Exists(folder))
                {
                    invalidFolders.Add(folder);
                }
            }

            foreach (var invalidFolder in invalidFolders)
            {
                syncFolders.Remove(invalidFolder);
                lstFolders.Items.Remove(invalidFolder);

                if (watchers.TryGetValue(invalidFolder, out var watcher))
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watchers.Remove(invalidFolder);
                }

                if (folderSyncDelays.TryGetValue(invalidFolder, out var cts))
                {
                    cts.Cancel();
                    folderSyncDelays.Remove(invalidFolder);
                }

                LogToFile($"Removed invalid folder from sync: {invalidFolder}");
            }

            appSettings.SyncFolders = syncFolders.ToList();
            SettingsManager.Save(appSettings);
        }

        private void lstFolders_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            // No-op or implement as needed
        }

        private void btnSaveServerUrls_Click(object sender, EventArgs e)
        {
            localServerUrl = cmbLocalServerUrl.Text.Trim();
            remoteServerUrl = cmbRemoteServerUrl.Text.Trim();
            appSettings.LocalServerUrl = localServerUrl;
            appSettings.RemoteServerUrl = remoteServerUrl;
            appSettings.JwtToken = txtApiKey.Text.Trim();
            SettingsManager.Save(appSettings);
            lblServerSettingsStatus.Text = "Settings saved.";
            MessageBox.Show("Server URLs and API Key saved successfully.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSetCurrentNetwork_Click(object sender, EventArgs e)
        {
            string detectedNetworkId = GetConnectedNetworkId();
            if (string.IsNullOrEmpty(detectedNetworkId))
            {
                MessageBox.Show("No active network detected.", "Set Local Network", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var result = MessageBox.Show($"Detected network ID: {detectedNetworkId}\nSet this as your local network?", "Set Local Network", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                currentNetworkId = detectedNetworkId;
                appSettings.CurrentNetworkId = currentNetworkId;
                SettingsManager.Save(appSettings);
                UpdateCurrentNetworkLabel();
                lblServerSettingsStatus.Text = "Local network set and saved.";
                MessageBox.Show("Local network set successfully.", "Set Local Network", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            _ = TestConnectionAsync();
        }

        private void lblApiKeyDesc_Click(object sender, EventArgs e)
        {
            // No-op or show help
        }

        private void btnResetSyncedFiles_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("This will clear the list of synced files and re-queue all files in your sync folders. Continue?", "Resync All Files", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                syncState.SyncedFiles = new HashSet<string>();
                SettingsManager.SaveSyncState(syncState);
                // Clear the queue UI and status before re-queueing
                dgvQueue.Rows.Clear();
                fileQueueStatus.Clear();
                queueManager.Clear();
                // Determine status for new items based on paused state
                string newStatus = appSettings.IsQueuePaused ? "Paused" : "Queued";
                // Re-queue all files
                foreach (var folderPath in syncFolders)
                {
                    if (Directory.Exists(folderPath))
                    {
                        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                        {
                            if (SupportedExtensions.Contains(Path.GetExtension(file)))
                            {
                                var workUnit = new WorkUnit(WorkUnitType.Upload, file);
                                queueManager.Enqueue(workUnit);
                                UpdateQueueStatus(file, newStatus);
                            }
                        }
                    }
                }
                if (!appSettings.IsQueuePaused)
                {
                    StartUploadWorker();
                }
                MessageBox.Show("All files have been re-queued for sync.", "Resync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string GetConnectedNetworkId()
        {
            try
            {
                var activeInterface = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                           (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet) &&
                                           ni.GetIPProperties().GatewayAddresses.Any());

                if (activeInterface != null)
                {
                    string macAddress = activeInterface.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(macAddress) && macAddress != "000000000000")
                    {
                        if (activeInterface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
                        {
                            return $"WIFI:{macAddress}";
                        }
                        if (activeInterface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
                        {
                            return $"ETH:{macAddress}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error getting Network ID: {ex.Message}");
            }

            return string.Empty;
        }

        private void SaveQueueState()
        {
            try
            {
                var queueList = queueManager.GetAllPending()
                    .Where(wu => wu.Status != WorkUnitStatus.Completed && wu.Status != WorkUnitStatus.Failed && wu.Status != WorkUnitStatus.InProgress && wu.Status != WorkUnitStatus.Cancelled)
                    .ToList();
                var json = System.Text.Json.JsonSerializer.Serialize(queueList);
                var dir = Path.GetDirectoryName(QueueStateFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(QueueStateFile, json);
            }
            catch (Exception ex)
            {
                LogToFile($"Error saving queue state: {ex.Message}");
            }
        }

        private void LoadQueueState()
        {
            try
            {
                // Clear the queue UI and status before loading
                dgvQueue.Rows.Clear();
                fileQueueStatus.Clear();
                if (File.Exists(QueueStateFile))
                {
                    var json = File.ReadAllText(QueueStateFile);
                    var queueList = System.Text.Json.JsonSerializer.Deserialize<List<WorkUnit>>(json);
                    if (queueList != null)
                    {
                        foreach (var wu in queueList)
                        {
                            // Only enqueue items that are not completed or cancelled
                            if (wu.Status == WorkUnitStatus.Completed || wu.Status == WorkUnitStatus.Cancelled)
                            {
                                // Show as Synced or Cancelled in the UI, but do not enqueue
                                if (wu.Status == WorkUnitStatus.Completed)
                                    UpdateQueueStatus(wu.FilePath, "Synced");
                                else if (wu.Status == WorkUnitStatus.Cancelled)
                                    UpdateQueueStatus(wu.FilePath, "Cancelled");
                                continue;
                            }
                            queueManager.Enqueue(wu);
                            // Map legacy/enum status to UI string, always show 'Queued' for queued items
                            string statusStr = wu.Status.ToString();
                            if (statusStr == "Queued" || statusStr == "Pending")
                            {
                                // If queue is paused, show as Paused
                                UpdateQueueStatus(wu.FilePath, appSettings.IsQueuePaused ? "Paused" : "Queued");
                            }
                            else if (statusStr == "Paused")
                                UpdateQueueStatus(wu.FilePath, "Paused");
                            else if (statusStr == "Failed")
                                UpdateQueueStatus(wu.FilePath, "Failed");
                            else
                                UpdateQueueStatus(wu.FilePath, statusStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading queue state: {ex.Message}");
            }
        }

        private void btnPauseResumeQueue_Click(object sender, EventArgs e)
        {
            // Toggle queue processing state (Pause/Resume)
            if (isUploading)
            {
                // Mark all in-progress and queued items as Paused
                foreach (DataGridViewRow row in dgvQueue.Rows)
                {
                    var status = row.Cells[1].Value as string;
                    var filePath = row.Cells[0].Value as string;
                    if ((status == "Queued" || status == "Syncing") && !string.IsNullOrEmpty(filePath))
                    {
                        // Find the WorkUnit and set status to Paused
                        var wu = queueManager.GetAllPending().FirstOrDefault(w => w.FilePath == filePath && (w.Status == WorkUnitStatus.Queued || w.Status == WorkUnitStatus.InProgress));
                        if (wu != null)
                        {
                            wu.Status = WorkUnitStatus.Paused;
                        }
                        row.Cells[1].Value = "Paused";
                        fileQueueStatus[filePath] = "Paused";
                    }
                }
                // Also handle currently processing (Syncing) items that are not in the queue
                foreach (var wu in currentBatch)
                {
                    if (wu.Status == WorkUnitStatus.Queued || wu.Status == WorkUnitStatus.InProgress)
                    {
                        wu.Status = WorkUnitStatus.Paused;
                        // Re-enqueue if not already in queue
                        if (!queueManager.GetAllPending().Any(q => q.FilePath == wu.FilePath))
                        {
                            queueManager.Enqueue(wu);
                        }
                    }
                }
                uploadCts.Cancel();
                isUploading = false;
                btnPauseResumeQueue.Text = "Resume Queue";
                appSettings.IsQueuePaused = true;
                SettingsManager.Save(appSettings);
            }
            else
            {
                // Resume all Paused items by setting their status back to Queued
                foreach (DataGridViewRow row in dgvQueue.Rows)
                {
                    var status = row.Cells[1].Value as string;
                    var filePath = row.Cells[0].Value as string;
                    if (status == "Paused" && !string.IsNullOrEmpty(filePath))
                    {
                        var wu = queueManager.GetAllPending().FirstOrDefault(w => w.FilePath == filePath);
                        if (wu != null)
                        {
                            wu.Status = WorkUnitStatus.Queued;
                        }
                        row.Cells[1].Value = "Queued";
                        fileQueueStatus[filePath] = "Queued";
                    }
                }
                uploadCts = new CancellationTokenSource();
                StartUploadWorker();
                btnPauseResumeQueue.Text = "Pause Queue";
                appSettings.IsQueuePaused = false;
                SettingsManager.Save(appSettings);
            }
        }

        private void btnCancelTask_Click(object sender, EventArgs e)
        {
            // Cancel the selected task in the queue
            if (dgvQueue.SelectedRows.Count > 0)
            {
                var selectedRow = dgvQueue.SelectedRows[0];
                var filePath = selectedRow.Cells[0].Value as string;
                var status = selectedRow.Cells[1].Value as string;

                if (!string.IsNullOrEmpty(filePath))
                {
                    if (status == "Synced" || status == "Completed")
                    {
                        MessageBox.Show("You can't cancel a task that was already successful.", "Cancel Task", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Set WorkUnit status to Cancelled if it exists in the queue
                    var wu = queueManager.GetAllPending().FirstOrDefault(w => w.FilePath == filePath);
                    if (wu != null)
                    {
                        wu.Status = WorkUnitStatus.Cancelled;
                    }
                    queueManager.RemoveTask(filePath);
                    UpdateQueueStatus(filePath, "Cancelled");
                }
            }
        }

        private void btnClearFailedTasks_Click(object sender, EventArgs e)
        {
            // Clear all failed tasks from the queue
            foreach (DataGridViewRow row in dgvQueue.Rows)
            {
                if (row.Cells[1].Value as string == "Failed")
                {
                    var filePath = row.Cells[0].Value as string;
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        queueManager.RemoveTask(filePath);
                        dgvQueue.Rows.Remove(row);
                    }
                }
            }
        }

        private void panelQueueButtons_Paint(object sender, PaintEventArgs e)
        {

        }

        // Helper to pause the queue programmatically
        private void PauseQueueProgrammatically()
        {
            if (isUploading)
            {
                // Mark all in-progress and queued items appropriately
                foreach (DataGridViewRow row in dgvQueue.Rows)
                {
                    var status = row.Cells[1].Value as string;
                    var filePath = row.Cells[0].Value as string ?? string.Empty;
                    if (status == "Syncing")
                    {
                        row.Cells[1].Value = "Failed";
                        fileQueueStatus[filePath] = "Failed";
                    }
                    else if (status == "Queued")
                    {
                        row.Cells[1].Value = "Paused";
                        fileQueueStatus[filePath] = "Paused";
                    }
                }
                pauseDueToError = true;
                uploadCts.Cancel();
                isUploading = false;
                btnPauseResumeQueue.Text = "Resume Queue";
            }
        }
    }
}
