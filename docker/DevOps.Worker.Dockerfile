FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props AI-DevOps-Platform.sln ./
COPY src/ src/

RUN dotnet publish src/DevOps.Worker/DevOps.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 9464
ENV ASPNETCORE_URLS=http://+:9464
ENV DOTNET_ENVIRONMENT=Development
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DevOps.Worker.dll"]
