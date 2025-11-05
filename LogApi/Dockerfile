FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY LogApi.csproj ./
RUN dotnet restore LogApi.csproj

# Copy everything else and build
COPY . ./
RUN dotnet clean LogApi.csproj
RUN dotnet restore LogApi.csproj --force
RUN dotnet publish LogApi.csproj -c Release -o /app/publish --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LogApi.dll"]