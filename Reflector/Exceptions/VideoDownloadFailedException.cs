namespace Reflector.Exceptions;

internal class VideoDownloadFailedException : Exception
{
    public VideoDownloadFailedException(string url) : base($"Unable to download {url}")
    {

    }
}