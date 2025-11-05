FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY LoadBalancer.csproj ./
RUN dotnet restore LoadBalancer.csproj

COPY . ./
RUN dotnet publish LoadBalancer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__Redis=localhost:6379

ENTRYPOINT ["dotnet", "LoadBalancer.dll"]