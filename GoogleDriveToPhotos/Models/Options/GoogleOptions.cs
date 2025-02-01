// Ignore Spelling: Api

namespace GoogleDriveToPhotosSync.Models.Options;

public class GoogleOptions
{
    public const string Name = "CasCap";
    public string MainFolderName { get; set; } = string.Empty;
    public int SyncFromMinutes { get; set; } = 5;
    public GooglePhotosOptions GooglePhotosOptions { get; set; } = new();
}
