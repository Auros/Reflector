using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reflector.Interfaces;
using Reflector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Reflector.Services;
internal class GQLTwitchVideoDownloader : IVideoDownloader
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ReflectorSettings _reflectorSettings;
    private static readonly Regex _clipRegex = new(@"twitch.tv\/(\S+)\/clip\/");

    public GQLTwitchVideoDownloader(ILogger<GQLTwitchVideoDownloader> logger, HttpClient httpClient, IHostEnvironment hostEnvironment, ReflectorSettings reflectorSettings)
    {
        _logger = logger;
        _httpClient = httpClient;
        _hostEnvironment = hostEnvironment;
        _reflectorSettings = reflectorSettings;
        _httpClient.DefaultRequestHeaders.Add("Client-ID", "kimne78kx3ncx6brgo4mv6wki5h1ko");
    }

    public async Task<Stream> DownloadAsync(string url)
    {
        var slug = ParseSlug(url);

        if (slug is null)
            throw new Exception("Could not parse slug from clip URL");

        CancellationTokenSource timeout = new((int)((_reflectorSettings.DownloadTimeoutInSeconds ?? 20f) * 1000));

        _logger.LogInformation("Fetching clip information for Twitch Clip {Slug}", slug);
        var content = new StringContent("[{\"operationName\":\"VideoAccessToken_Clip\",\"variables\":{\"slug\":\"" + slug + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"36b89d2507fce29e5ca551df756d27c1cfe079e2609642b4390aa4c35796eb11\"}}}]", Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("https://gql.twitch.tv/gql", content, cancellationToken: timeout.Token);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Could not fetch clip data.");

        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Fetched clip data for {Slug}", slug);

        using var stream = response.Content.ReadAsStream();
        var doc = JsonDocument.Parse(stream);

        var clipElement = doc.RootElement[0].GetProperty("data").GetProperty("clip");
        var playbackAccessToken = clipElement.GetProperty("playbackAccessToken");
        var videoQualities = clipElement.GetProperty("videoQualities");

        var download = videoQualities[0].GetProperty("sourceURL").GetString();
        download += "?sig=" + playbackAccessToken.GetProperty("signature").GetString() + "&token=" + HttpUtility.UrlEncode(playbackAccessToken.GetProperty("value").GetString());

        _logger.LogInformation("Starting download process for {Url}", url);

        var videoStream = await _httpClient.GetStreamAsync(download, cancellationToken: timeout.Token);
        _logger.LogInformation("Downloaded clip with slug {Slug} at {Url}", slug, url);
        if (_reflectorSettings.DeleteLocalDownloads ?? true)
        {
            // If the user wants to delete the file, we don't do anything as we don't save it with this implementation
            return videoStream;
        }

        // Move the data to a memory stream, then save that to a file stream, and return the original memory stream
        // We need to make a separate memory stream as the http content stream is not seekable, and can only be read once.

        // Create the temporary folder if it doesn't exist.
        DirectoryInfo saveDir = new(Path.Combine(_hostEnvironment.ContentRootPath, _reflectorSettings.DownloadFolderPath ?? "Reflector Downloads"));
        if (!saveDir.Exists)
        {
            _logger.LogInformation("The temporary cache folder {SaveDirectory} doesn't exist, creating...", saveDir.FullName);
            saveDir.Create();
        }

        var idFileName = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "_" + slug + ".mp4";
        var path = Path.Combine(saveDir.FullName, idFileName);
        MemoryStream memoryStream = new();
        await videoStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        _logger.LogInformation("Saving downloaded file to {File}", path);
        await using var fileStream = File.OpenWrite(Path.Combine(saveDir.FullName, idFileName));
        await memoryStream.CopyToAsync(fileStream);
        memoryStream.Position = 0;
        await videoStream.DisposeAsync();
        return memoryStream;
    }

    static string? ParseSlug(string text)
    {
        if (text.All(char.IsLetter))
            return text;
        
        if (text.Contains("clips.twitch.tv/") || _clipRegex.IsMatch(text))
        {
            Uri url = new UriBuilder(text).Uri;
            string path = string.Format("{0}{1}{2}{3}", url.Scheme, Uri.SchemeDelimiter, url.Authority, url.AbsolutePath);
            return path.Split('/').Last();
        }
        return null;
    }
}
