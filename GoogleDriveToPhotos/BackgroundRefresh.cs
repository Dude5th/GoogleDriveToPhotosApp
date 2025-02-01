using GoogleDriveToPhotosSync.Models.Options;
using GoogleDriveToPhotosSync.Services;
using Microsoft.Extensions.Options;

namespace GoogleDriveToPhotosSync;

public sealed class BackgroundRefresh(ILogger<BackgroundRefresh> logger, IOptions<GoogleOptions> options, GoogleDriveService googleDriveService, GooglePhotoService googlePhotoService)
    : IHostedService, IDisposable
{
    private readonly ILogger<BackgroundRefresh> _logger = logger;
    private readonly GoogleOptions _googleOptions = options.Value;
    private readonly GoogleDriveService _googleDriveService = googleDriveService;
    private readonly GooglePhotoService _googlePhotoService = googlePhotoService;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(TransferFiles, null, TimeSpan.Zero, TimeSpan.FromMinutes(_googleOptions.SyncFromMinutes));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void TransferFiles(object? state)
    {
        var mainFolder = _googleOptions.MainFolderName;
        if (string.IsNullOrEmpty(mainFolder))
        {
            throw new InvalidDataException($"{nameof(_googleOptions.MainFolderName)} was not set in appsettings.json. Please make sure to set all the required fields.");
        }

        _logger.LogInformation("Starting Sync");
        var photoFileNames = await _googlePhotoService.GetFilesInAlbum(mainFolder);
        _logger.LogInformation("Found {Count} files in Google Photos folder '{Folder}'", photoFileNames.Count, mainFolder);

        var driveFolders = await _googleDriveService.GetFilesInWeddingFolderAsync(mainFolder);
        _logger.LogInformation("Found {Count} files in Google Drives folder '{Folder}'", photoFileNames.Count, mainFolder);

        _logger.LogInformation("Check missing files between the drive and photos");
        Dictionary<string, List<Google.Apis.Drive.v3.Data.File>> files = [];
        foreach (var folder in driveFolders)
        {
            var folderName = folder.Key;
            var folderFileNames = folder.Value.Select(s => s.Name.ToFullFolderFileName(folderName)).ToList();
            if (folderFileNames.Count > 0)
            {
                var filesMisisng = folderFileNames.Except(photoFileNames).ToList();
                files.Add(folderName, folder.Value.Where(s => filesMisisng.Contains(s.Name.ToFullFolderFileName(folderName))).ToList());
            }
        }

        _logger.LogInformation("Missing files {MissingFiles} from Google Photos", files.Values.Count);
        var paths = await _googleDriveService.DownloadFilesAsync(files);

        _logger.LogInformation("Uploading {Count} files to Google Photos", paths.Count);
        await _googlePhotoService.UploadFilesAsync(paths, mainFolder);
        _logger.LogInformation("Finished Sync");
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}