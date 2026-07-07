FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FindAirConditioners.sln ./
COPY Directory.Build.props ./
COPY src/FindAirConditioners.Domain/FindAirConditioners.Domain.csproj src/FindAirConditioners.Domain/
COPY src/FindAirConditioners.Application/FindAirConditioners.Application.csproj src/FindAirConditioners.Application/
COPY src/FindAirConditioners.Infrastructure/FindAirConditioners.Infrastructure.csproj src/FindAirConditioners.Infrastructure/
COPY src/FindAirConditioners.Web/FindAirConditioners.Web.csproj src/FindAirConditioners.Web/
COPY tests/FindAirConditioners.UnitTests/FindAirConditioners.UnitTests.csproj tests/FindAirConditioners.UnitTests/
COPY tests/FindAirConditioners.IntegrationTests/FindAirConditioners.IntegrationTests.csproj tests/FindAirConditioners.IntegrationTests/
RUN dotnet restore FindAirConditioners.sln

COPY . .
RUN dotnet publish src/FindAirConditioners.Web/FindAirConditioners.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-jammy AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "FindAirConditioners.Web.dll"]
