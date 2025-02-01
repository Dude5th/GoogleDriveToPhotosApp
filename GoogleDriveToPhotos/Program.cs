using GoogleDriveToPhotosSync;
using GoogleDriveToPhotosSync.Models.Options;
using GoogleDriveToPhotosSync.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Get options
IConfigurationSection googleOptions = builder.Configuration.GetSection(GoogleOptions.Name);

// Configure Options
builder.Services.Configure<GoogleOptions>(googleOptions);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GoogleDriveService>();
builder.Services.AddSingleton<GooglePhotoService>();
builder.Services.AddHostedService<BackgroundRefresh>();

builder.Services.AddGooglePhotos(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var driveTags = new List<OpenApiTag> { new() { Name = "Drive" } };
app.MapGet("/drive/messages", (GoogleDriveService service) => service.GoogleDriveMessages)
    .WithOpenApi(o => { o.Tags = driveTags; return o; });
app.MapGet("/drive/folders", (GoogleDriveService service) => service.GetFoldersAsync())
    .WithOpenApi(o => { o.Tags = driveTags; return o; });
app.MapGet("/drive/images", (GoogleDriveService service) => service.GetImagesAsync())
    .WithOpenApi(o => { o.Tags = driveTags; return o; });

var photoTags = new List<OpenApiTag> { new() { Name = "Photo" } };
app.MapGet("/photo/albums", (GooglePhotoService service) => service.GetAlbumsAsync())
    .WithOpenApi(o => { o.Tags = photoTags; return o; });
app.MapGet("/photo/messages", (GooglePhotoService service) => service.GooglePhotoMessages)
    .WithOpenApi(o => { o.Tags = photoTags; return o; });

app.Run();