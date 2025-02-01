# GoogleDriveToPhotosApp

This App is meant to Sync a Folder of images and videos on Google Drive to Google Photos.

## Setup
To use this app, you will need to setup with the following:
- Log into Google Cloud Console and create a Project
- Create Credentials using OAuth 2.0
- Install Google Drive API library
- Install Google Photos API library
- Update the appsettings.json with your client ID, Secret, User and Folder in which you would like to sync

## Running the application
  
If using VS, hit F5 or hte play button Or dotnet run command.
The background task will run the sync.
You can use the APIs to see messages coming through.
