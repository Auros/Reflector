# Reflector
A discord bot which automatically downloads and sends Twitch clips sent in a channel

## Configuration

Create an appsettings.json file and provide the following
```jsonc
"ConnectionStrings": {
  "Token": "DISCORD BOT TOKEN" // Required
},
"Reflector": { // Optional
  "DeleteLocalDownloads": true, // (boolean) Optional, defaults to true. This will not save local downloads. 
  "DownloadTimeoutInSeconds": 20, // (float) Optional, defaults to 20 seconds
  "AllowedChannels": [], // (ulong[]) Optional, if this list has a discord channel ID in it, the bot will only work in those channels. If empty, it will work in all channels 
  "DownloadFolderPath": "Reflector Downloads", // (string) Optional, the relative or absolute path to a folder to save downloads to. If DeleteLocalDownloads is false, files will be saved here.
}
```

## Docker
There is a [Dockerfile](https://github.com/Auros/Reflector/blob/main/Reflector/Dockerfile) available for easier deployment if using Docker. Just pull the repository and build the Dockerfile

You can setup a volume linking to `/app/appsettings.json` to link your settings file, or you can just load environment variables normally [like this](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#naming-of-environment-variables)
