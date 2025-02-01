using System.Collections.Concurrent;
using CasCap.Models;
using CasCap.Services;

namespace GoogleDriveToPhotosSync.Services
{
    public class GooglePhotoService(ILogger<GooglePhotoService> logger, GooglePhotosService googlePhotosSvc)
    {
        private readonly GooglePhotosService _googlePhotosSvc = googlePhotosSvc;
        private readonly ILogger<GooglePhotoService> _logger = logger;
        public ConcurrentBag<string> GooglePhotoMessages { get; set; } = [];

        public async Task<List<string>> GetAlbumsAsync()
        {
            if (!await _googlePhotosSvc.LoginAsync())
                throw new InvalidDataException($"login failed");

            AddMessage("Login successful");
            List<string> list = [];
            var albums = await _googlePhotosSvc.GetAlbumsAsync();

            AddMessage($"GooglePhotoService: Found {albums.Count} albums");
            foreach (var album in albums)
            {
                list.Add($"Album: {album.title}, ID: {album.id}");
            }

            return list;
        }

        public async Task<List<string>> GetFilesInAlbum(string albumName)
        {
            AddMessage("Logging in to Google Photos");
            if (!await _googlePhotosSvc.LoginAsync())
                throw new InvalidDataException($"login failed");

            Album? album = await GetAlbum(albumName);
            if (album == null)
            {
                return [];
            }

            AddMessage($"Getting files in folder '{albumName}'");
            var items = _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id);
            if (items == null)
            {
                AddError($"Could not find media items in album: {album.title}");
                return [];
            }

            return await items.Select(s => s.filename).ToListAsync();
        }

        public async Task UploadFilesAsync(List<string> filePaths, string mainFolder)
        {
            Album? album = await GetAlbum(mainFolder);
            if (album == null)
            {
                return;
            }

            foreach (var filePath in filePaths)
            {
                var upload = await _googlePhotosSvc.UploadSingle(filePath, album.id);
                if (upload != null)
                {
                    AddMessage($"Name: {filePath.Split("/")[^1]}, Code: {upload.status.code}, Status: {upload.status.status}, Message: {upload.status.message}");
                }
            }
        }

        private void AddError(string text)
        {
            _logger.LogError("{Date} - {Text}", DateTime.Now.ToLongTimeString(), text);
            GooglePhotoMessages.Add($"{DateTime.Now.ToLongTimeString()} - {text}");
        }

        private void AddMessage(string text)
        {
            _logger.LogInformation("{Date} - {Text}", DateTime.Now.ToLongTimeString(), text);
            GooglePhotoMessages.Add($"{DateTime.Now.ToLongTimeString()} - {text}");
        }

        private async Task<Album?> GetAlbum(string albumName)
        {
            AddMessage($"Getting Album folder for '{albumName}'.");
            Album? album = await _googlePhotosSvc.GetAlbumByTitleAsync(albumName);
            if (album == null)
            {
                AddError($"Could not find album '{albumName}', creating a new album.");
                album = await _googlePhotosSvc.CreateAlbumAsync(albumName);
            }

            return album;
        }
    }
}