namespace GoogleDriveToPhotosSync
{
    public static class Constants
    {
        public static readonly string FilePath = Directory.GetCurrentDirectory() + "/drive_files/";

        public static string ToFullFolderFileName(this string fileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return fileName;
            }

            return $"{folderName}-{fileName}";
        }
    }
}
