using DisCatSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reflector.Daemons;
using Serilog;
using Serilog.Extensions.Logging;

Console.Title = nameof(Reflector);

const string template = "[{Timestamp:HH:mm:ss} | {Level:u3} | {SourceContext}] {Message:lj}{NewLine}{Exception}";

var loggerConfig = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: template)
    .MinimumLevel.Information()
    ;

var logger = loggerConfig.CreateLogger();
var loggerFactory = new SerilogLoggerFactory(logger, true);
Log.Logger = logger;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureLogging((ctx, builder) =>
{
    builder.ClearProviders();
    builder.AddSerilog(logger);
});

builder.ConfigureServices((ctx, services) =>
{
    var token = ctx.Configuration.GetConnectionString("Token");

    /* Discord Configuration */
    /* https://discordapi.com/permissions.html#117824 */
    DiscordConfiguration discordConfig = new()
    {
        Token = token,
        TokenType = TokenType.Bot,
        LoggerFactory = loggerFactory,
        MinimumLogLevel = LogLevel.Information,
    };

    services
        .AddSingleton<DiscordClient>(_ => new DiscordClient(discordConfig))
        .AddHostedService<ResponderDaemon>()
        .AddHostedService<BotDaemon>()
        ;

});

var host = builder.UseConsoleLifetime(o => o.SuppressStatusMessages = true).Build();
await host.RunAsync();