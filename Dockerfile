FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/ApiPulse/ApiPulse.csproj src/ApiPulse/
RUN dotnet restore src/ApiPulse/ApiPulse.csproj
COPY src/ApiPulse/ src/ApiPulse/
RUN dotnet publish src/ApiPulse/ApiPulse.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiPulse.dll"]