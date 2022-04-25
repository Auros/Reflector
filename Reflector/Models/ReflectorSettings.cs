namespace Reflector.Models;

internal class ReflectorSettings
{
    public ulong[] AllowedChannels { get; set; } = Array.Empty<ulong>();
    public string? YoutubeDLPath { get; set; }
}