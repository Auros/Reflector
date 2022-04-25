using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reflector.Exceptions;
using Reflector.Interfaces;
using Reflector.Models;
using System.Diagnostics;

namespace Reflector.Services;

internal class YoutubeDLVideoDownloader : IVideoDownloader
{
    private readonly ILogger _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ReflectorSettings _reflectorSettings;

    public YoutubeDLVideoDownloader(ILogger<YoutubeDLVideoDownloader> logger, IHostEnvironment hostEnvironment, ReflectorSettings reflectorSettings)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _reflectorSettings = reflectorSettings;
    }

    public async Task<Stream> DownloadAsync(string url)
    {
        if (string.IsNullOrEmpty(_reflectorSettings.YoutubeDLPath))
        {
            throw new Exception("Youtube DL path not provided. Unable to download videos.");
        }

        // Create the temporary folder if it doesn't exist.
        DirectoryInfo saveDir = new(Path.Combine(_hostEnvironment.ContentRootPath, _reflectorSettings.DownloadFolderPath ?? "Reflector Downloads"));
        if (!saveDir.Exists)
        {
            _logger.LogInformation("The temporary cache folder {SaveDirectory} doesn't exist, creating...", saveDir.FullName);
            saveDir.Create();
        }

        var idFileName = Guid.NewGuid().ToString() + ".mp4";

        ProcessStartInfo startInfo = new()
        {
            FileName = _reflectorSettings.YoutubeDLPath,
            Arguments = $"{url} -o {idFileName}",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = saveDir.FullName
        };

        Process process = new() { StartInfo = startInfo };

        _logger.LogInformation("Starting download process for {Url}", url);
        CancellationTokenSource timeout = new((int)((_reflectorSettings.DownloadTimeoutInSeconds ?? 20f) * 1000));

        try
        {
            process.Start();
            await process.WaitForExitAsync(timeout.Token);
        }
        catch
        {
            _logger.LogError("An error occurred while trying to download the video {VideoUrl}", url);
            throw;
        }

        _logger.LogInformation("Completed video download for {Url}", url);

        // Load the file completely into memory
        var file = saveDir.GetFiles(idFileName).FirstOrDefault();
        if (file is null)
            throw new VideoDownloadFailedException(url);

        var fileStream = file.OpenRead();
        if (_reflectorSettings.DeleteLocalDownloads ?? true)
        {
            // If the user wants to delete the file, we copy it over to a memory stream then delete it.
            MemoryStream memoryStream = new();
            await fileStream.CopyToAsync(memoryStream);
            await fileStream.DisposeAsync();
            file.Delete(); // Delete the old file if requested.

            memoryStream.Position = 0; // Reset the stream as writing to it puts the header at the end.
            return memoryStream;
        }

        // Since the user wants to save files to the download cache, we only return the file stream
        return fileStream;
    }
}
