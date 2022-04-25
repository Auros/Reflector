using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reflector.Exceptions;
using Reflector.Models;
using System.Diagnostics;

namespace Reflector.Services;

internal interface IVideoDownloader
{
    Task<FileStream> DownloadAsync(string url, string? name = null);
}

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

    public async Task<FileStream> DownloadAsync(string url, string? name = null)
    {
        if (string.IsNullOrEmpty(_reflectorSettings.YoutubeDLPath))
        {
            throw new Exception("Youtube DL path not provided. Unable to download videos.");
        }

        // Create the temporary folder if it doesn't exist.

        DirectoryInfo saveDir = new(Path.Combine(_hostEnvironment.ContentRootPath, "Temporary Download Cache"));
        if (!saveDir.Exists)
        {
            _logger.LogInformation("The temporary cache folder {SaveDirectory} doesn't exist, creating...", saveDir.FullName);
            saveDir.Create();
        }

        var idFileName = name ?? Guid.NewGuid().ToString() + ".mp4";

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

        _logger.LogInformation("Starting download process for {URL}", url);

        CancellationTokenSource timeout = new(20_000); // 20 second timeout, TODO: maybe make this a config option

        try
        {
            process.OutputDataReceived += Process_OutputDataReceived;
            process.Start();
            await process.WaitForExitAsync(timeout.Token);
        }
        catch
        {
            _logger.LogError("An error occurred while trying to download the video {VideoUrl}", url);
            throw;
        }
        finally
        {
            process.OutputDataReceived -= Process_OutputDataReceived;
        }

        // Load the file completely into memory
        var file = saveDir.GetFiles(idFileName).FirstOrDefault();
        if (file is null)
            throw new VideoDownloadFailedException(url);

        return file.OpenRead();
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        _logger.LogInformation("Data Received from Process: {Message}", e.Data);
    }
}
