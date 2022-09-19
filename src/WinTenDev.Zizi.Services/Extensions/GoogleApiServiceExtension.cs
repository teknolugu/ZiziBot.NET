using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using HeyRed.Mime;
using WinTenDev.Zizi.Services.Google;
using File=Google.Apis.Drive.v3.Data.File;

namespace WinTenDev.Zizi.Services.Extensions;

public static class GoogleApiServiceExtension
{
    public static async Task<string> CreateFolderOnGoogleDrive(
        this GoogleApiService api,
        string rootParentId,
        string locationPath
    )
    {
        var credential = api.GetDefaultServiceAccount();

        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        var parentId = rootParentId;
        foreach (var path in locationPath.Split("/"))
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and trashed = false and '{parentId}' in parents";
            listRequest.SupportsAllDrives = true;
            listRequest.Corpora = "allDrives";
            listRequest.IncludeItemsFromAllDrives = true;
            listRequest.Fields = "nextPageToken, files(id,name,parents,size,mimeType,quotaBytesUsed,modifiedTime,exportLinks,webContentLink,webViewLink)";
            var listFile = await listRequest.ExecuteAsync();

            var existingDir = listFile.Files.FirstOrDefault(file => file.Name == path);
            if (existingDir != null)
            {
                parentId = existingDir.Id;
            }
            else
            {
                var fileMetadata = new File()
                {
                    Name = path,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new[] { parentId }
                };

                Log.Debug("Creating Google Drive folder: {Name} on Parent: {ParentId}", path, parentId);
                var request = service.Files.Create(fileMetadata);
                request.SupportsAllDrives = true;
                request.Fields = "id, name, parents, createdTime, modifiedTime, mimeType";

                var result = await request.ExecuteAsync();

                parentId = result.Id;
            }
        }

        return parentId;
    }

    public static async Task<File> UploadFileToGoogleDrive(
        this GoogleApiService api,
        string parentId,
        string sourceFile,
        string locationPath = null,
        bool preventDuplicate = false,
        Func<CallbackAnswer, Task> answer = null
    )
    {
        var config = api.GetConfig();

        Log.Information("Uploading file to Google Drive. Source: {SourceFile}", sourceFile);
        var credential = api.GetDefaultServiceAccount();

        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        string fileName;
        Stream streamSource;

        if (sourceFile.StartsWith("http"))
        {
            fileName = await sourceFile.GetServerFileName();
            Log.Debug("Source file from URL. FileName: {FileName}", fileName);
            streamSource = await sourceFile.OpenFlurlSession().GetStreamAsync();
        }
        else
        {
            Log.Debug("Source file from local. FileName: {FileName}", sourceFile);
            fileName = Path.GetFileName(sourceFile);
            streamSource = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
        }

        var parent = parentId;
        if (locationPath != null)
        {
            parent = config.ZiziBotDrive;
            parent = await api.CreateFolderOnGoogleDrive(parent, locationPath);
        }

        var fileMetadata = new File()
        {
            Name = fileName,
            Description = "Uploaded by ZiziBot",
            Parents = new List<string>()
            {
                parent
            }
        };

        var mimeType = MimeTypesMap.GetMimeType(fileName);
        var request = service.Files.Create(fileMetadata, streamSource, mimeType);
        request.SupportsAllDrives = true;

        request.ProgressChanged += delegate(IUploadProgress progress) {
            Log.Verbose("Progress: {Progress}", progress);

            answer?.Invoke(new CallbackAnswer()
            {
            });
        };
        request.ResponseReceived += delegate(File file) {
            Log.Debug("Response: {@File}", file);

            answer?.Invoke(new CallbackAnswer()
            {
            });
        };

        var results = await request.UploadAsync(CancellationToken.None);

        if (results.Status == UploadStatus.Failed)
        {
            Log.Debug("Error uploading file: {Message}", results.Exception.Message);

            answer?.Invoke(new CallbackAnswer()
            {
            });
        }

        var uploadedFileId = request.ResponseBody;

        streamSource.Close();

        return uploadedFileId;
    }
}