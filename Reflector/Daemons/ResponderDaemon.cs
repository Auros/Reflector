using DisCatSharp;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Reflector.Daemons;

internal class ResponderDaemon : IHostedService
{
    private readonly ILogger _logger;
    private readonly DiscordClient _discordClient;

    private static readonly Regex _twitchUrlRegex = new(@"https:\/\/(?:clips|www)\.twitch\.tv\/(?:(?:[a-z]+)\/clip\/)?([a-zA-Z]+)([^\s]+)", RegexOptions.IgnoreCase);

    public ResponderDaemon(ILogger<ResponderDaemon> logger, DiscordClient discordClient)
    {
        _logger = logger;
        _discordClient = discordClient;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _discordClient.MessageCreated += DiscordClient_MessageCreated;
        return Task.CompletedTask;
    }

    private Task DiscordClient_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        var match = _twitchUrlRegex.Match(e.Message.Content);
        _logger.LogInformation("RegexIsMatch = {IsMatch}, Match = {MatchedContext}", match != Match.Empty, match == Match.Empty ? "None" : match.Value);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.MessageCreated -= DiscordClient_MessageCreated;
        return Task.CompletedTask;
    }
}