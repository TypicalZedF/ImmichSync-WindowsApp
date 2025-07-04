public class AppSettings {
    public string LocalServerUrl { get; set; } = string.Empty;
    public string RemoteServerUrl { get; set; } = string.Empty;
    public string CurrentNetworkId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public List<string> SyncFolders { get; set; } = new List<string>();
    public bool PutEachFileInOwnAlbum { get; set; } = false;
    public bool StartMinimizedAtBoot { get; set; } = false;
    public bool CloseButtonMinimizes { get; set; } = true;
    // Add this property for concurrent uploads
    public int MaxConcurrentUploads { get; set; } = 3;
}