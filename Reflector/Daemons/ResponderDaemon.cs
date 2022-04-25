using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reflector.Exceptions;
using Reflector.Interfaces;
using Reflector.Models;
using System.Text.RegularExpressions;

namespace Reflector.Daemons;

internal class ResponderDaemon : IHostedService
{
    private readonly ILogger _logger;
    private readonly DiscordClient _discordClient;
    private readonly IVideoDownloader _videoDownloader;
    private readonly ReflectorSettings _reflectorSettings;

    private static readonly Regex _twitchUrlRegex = new(@"https:\/\/(?:clips|www)\.twitch\.tv\/(?:(?:[a-z]+)\/clip\/)?([a-zA-Z]+)([^\s]+)", RegexOptions.IgnoreCase);

    public ResponderDaemon(ILogger<ResponderDaemon> logger, DiscordClient discordClient, IVideoDownloader videoDownloader, ReflectorSettings reflectorSettings)
    {
        _logger = logger;
        _discordClient = discordClient;
        _videoDownloader = videoDownloader;
        _reflectorSettings = reflectorSettings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _discordClient.MessageCreated += DiscordClient_MessageCreated;
        return Task.CompletedTask;
    }

    private Task DiscordClient_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        if (e.Author.IsBot)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                // If there are explicit channels to look for, we analyze it here.
                if (_reflectorSettings.AllowedChannels.Length > 0)
                {
                    if (!_reflectorSettings.AllowedChannels.Contains(e.Channel.Id))
                        return;
                }

                var match = _twitchUrlRegex.Match(e.Message.Content);

                if (match != Match.Empty)
                {
                    try
                    {
                        await e.Channel.TriggerTypingAsync();
                        await using var videoStream = await _videoDownloader.DownloadAsync(match.Value);

                        var builder = new DiscordMessageBuilder()
                            .WithFile(match.Value.Replace("https://clips.twitch.tv/", string.Empty).Replace("https://www.twitch.tv/", string.Empty) + ".mp4", videoStream);

                        await e.Channel.SendMessageAsync(builder);
                    }
                    catch (ProgramDoesNotExistException pe)
                    {
                        _logger.LogError("{Exception}", pe);
                    }
                    catch (VideoDownloadFailedException ve)
                    {
                        _logger.LogError("{Exception}", ve);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical("An unhandled exception has occured.\n{Exception}", e);
                    }
                }
            }
            catch
            {
                throw;
            }
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.MessageCreated -= DiscordClient_MessageCreated;
        return Task.CompletedTask;
    }
}