FROM python:3.7.1-alpine
WORKDIR /dep
RUN pip install --upgrade pip && pip --no-cache-dir install --no-deps -U yt-dlp

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app .
ENTRYPOINT ["dotnet", "Reflector.dll"]