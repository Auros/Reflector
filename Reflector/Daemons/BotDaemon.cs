using DisCatSharp;
using Microsoft.Extensions.Hosting;

namespace Reflector.Daemons;

internal class BotDaemon : IHostedService
{
    private readonly DiscordClient _discordClient;

    public BotDaemon(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discordClient.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discordClient.DisconnectAsync();
    }
}