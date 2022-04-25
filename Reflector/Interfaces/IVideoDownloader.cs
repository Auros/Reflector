namespace Reflector.Interfaces;

internal interface IVideoDownloader
{
    Task<Stream> DownloadAsync(string url);
}