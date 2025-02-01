using System.Collections.Concurrent;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using GoogleDriveToPhotosSync.Models.Options;
using Microsoft.Extensions.Options;

namespace GoogleDriveToPhotosSync.Services;

public class GoogleDriveService(ILogger<GoogleDriveService> logger, IOptions<GoogleOptions> options)
{
    private const string FolderQuery = "mimeType='application/vnd.google-apps.folder'";
    private readonly ILogger<GoogleDriveService> _logger = logger;
    private readonly GoogleOptions _options = options.Value;
    private UserCredential? userCredential;

    public ConcurrentBag<string> GoogleDriveMessages { get; set; } = [];

    public async Task<List<string>> DownloadFilesAsync(Dictionary<string, List<Google.Apis.Drive.v3.Data.File>> folders)
    {
        AddMessage("Download files from Drive.");
        using DriveService driveService = await GetServiceAsync();
        List<string> paths = [];
        foreach (var folder in folders)
        {
            var folderName = folder.Key;
            var files = folder.Value;
            foreach (var file in files)
            {
                AddMessage($"Downloading {file.Name} from {folderName}.");
                // download file
                var operation = driveService.Files.Get(file.Id);
                if (operation == null)
                {
                    AddError($"Failed to download {file.Name} from {folderName}.");
                    continue;
                }

                if (!Directory.Exists(Constants.FilePath))
                {
                    AddMessage($"Creating directory: {Constants.FilePath}.");
                    Directory.CreateDirectory(Constants.FilePath);
                }

                var newFileName = file.Name.ToFullFolderFileName(folderName);
                var filePath = $"{Constants.FilePath}/{newFileName}";
                paths.Add(filePath);
                if (System.IO.File.Exists(filePath))
                {
                    AddMessage($"{file.Name} already exists, skipping download.");
                    continue;
                }

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await operation.DownloadAsync(fileStream);
                AddMessage($"Downloaded: {newFileName} to {Constants.FilePath}.");
            }
        }

        return paths;
    }

    public async Task<Dictionary<string, IList<Google.Apis.Drive.v3.Data.File>>> GetFilesInWeddingFolderAsync(string mainFolder)
    {
        GoogleDriveMessages = [];
        var data = new Dictionary<string, IList<Google.Apis.Drive.v3.Data.File>>();

        AddMessage("Getting folders from Google Drive");
        IList<Google.Apis.Drive.v3.Data.File> allFolders = await GetData(FolderQuery);

        AddMessage($"Finding the main folder of '{mainFolder}'");
        var parentFolder = allFolders
            .FirstOrDefault(s => s.Name.Equals(mainFolder));
        if (parentFolder == null)
        {
            AddMessage($"{mainFolder} folder was not found, returning empty list.");
            return [];
        }

        AddMessage($"Getting files from the folder: '{mainFolder}'");
        var parentQuery = GetFolderChildrenById(parentFolder.Id);
        var fileQuery = $"{parentQuery} and not ({FolderQuery})";
        var parentFolderFiles = await GetData(fileQuery);
        AddMessage($"Found {parentFolderFiles.Count} files in {mainFolder} folder");
        foreach (var file in parentFolderFiles)
        {
            data.Add(string.Empty, parentFolderFiles);
        }

        var query = $"{parentQuery} and ({FolderQuery})";
        var folders = await GetData(query);
        foreach (var folder in folders)
        {
            // get files in folder
            parentQuery = GetFolderChildrenById(folder.Id);
            var filesQuery = $"{parentQuery} and not ({FolderQuery})";
            var files = await GetData(filesQuery);
            data.Add(folder.Name, files);
            AddMessage($"Found {files.Count} files in {folder.Name} folder");
        }

        return data;
    }

    public async Task<List<string>> GetFoldersAsync()
    {
        IList<Google.Apis.Drive.v3.Data.File> folders = await GetData("mimeType='application/vnd.google-apps.folder'");

        List<string> list = [];
        foreach (var folder in folders)
        {
            list.Add($"{folder.Name} - {folder.Kind} - {folder.MimeType}");
        }

        return list;
    }

    public async Task<List<string>> GetImagesAsync()
    {
        using DriveService driveService = await GetServiceAsync();
        var files = driveService
            .Files
            .List();

        files.Q = "mimeType='image/jpeg' or mimeType='image/png'";

        AddMessage("Grabbing Images from Drive");
        var listFilesRequest = files.Execute();
        if (listFilesRequest.Files == null)
        {
            return [];
        }

        List<string> list = [];
        foreach (var file in listFilesRequest.Files)
        {
            list.Add($"{file.Name} - {file.Kind} - {file.MimeType}");
        }

        return list;
    }

    private static string GetFolderChildrenById(string folderId) => $"'{folderId}' in parents";

    private void AddError(string text)
    {
        _logger.LogError("{Date} - {Text}", DateTime.Now.ToLongTimeString(), text);
        GoogleDriveMessages.Add($"{DateTime.Now.ToLongTimeString()} - {text}");
    }

    private void AddMessage(string text)
    {
        _logger.LogInformation("{Date} - {Text}", DateTime.Now.ToLongTimeString(), text);
        GoogleDriveMessages.Add($"{DateTime.Now.ToLongTimeString()} - {text}");
    }

    private async Task<IList<Google.Apis.Drive.v3.Data.File>> GetData(string? query)
    {
        using DriveService driveService = await GetServiceAsync();
        var files = driveService
            .Files
            .List();

        if (query is not null)
        {
            files.Q = query;
        }

        files.OrderBy = "name";

        AddMessage("Grabbing data from Drive");
        FileList listFilesRequest = files.Execute();
        if (listFilesRequest.Files == null)
        {
            return [];
        }

        return listFilesRequest.Files;
    }

    private async Task<DriveService> GetServiceAsync()
    {
        if (userCredential is null)
        {
            AddMessage("Logging in to Google");
            userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets()
            {
                ClientId = _options.GooglePhotosOptions.ClientId,
                ClientSecret = _options.GooglePhotosOptions.ClientSecret
            }, [DriveService.Scope.Drive], "user", CancellationToken.None);

            AddMessage("Logged in to Google");
        }

        return new(new BaseClientService.Initializer
        {
            HttpClientInitializer = userCredential,
            ApplicationName = "GoogleDriveToPhotosApp"
        });
    }
}