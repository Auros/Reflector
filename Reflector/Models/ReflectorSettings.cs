namespace Reflector.Models;

internal class ReflectorSettings
{
    public ulong[] AllowedChannels { get; set; } = Array.Empty<ulong>();
    public string? DownloadFolderPath { get; set; } = "Reflector Downloads";
    public bool? DeleteLocalDownloads { get; set; } = true;
    public float? DownloadTimeoutInSeconds { get; set; } = 20f;
    public string? YoutubeDLPath { get; set; }
}