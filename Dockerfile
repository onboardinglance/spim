
# 1. Use the BUILDER'S native architecture for the SDK to prevent emulation crashes
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Disable telemetry just to be safe
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV OPENSSL_FORCE_FIPS_MODE=0

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Spim.Domain/Spim.Domain.csproj", "src/Spim.Domain/"]
COPY ["src/Spim.Application/Spim.Application.csproj", "src/Spim.Application/"]
COPY ["src/Spim.Infrastructure/Spim.Infrastructure.csproj", "src/Spim.Infrastructure/"]
COPY ["src/Spim.Persistence/Spim.Persistence.csproj", "src/Spim.Persistence/"]
COPY ["src/Spim.Web/Spim.Web.csproj", "src/Spim.Web/"]
# 2. Tell .NET to restore packages specifically for the target architecture
RUN dotnet restore "src/Spim.Web/Spim.Web.csproj" -a $TARGETARCH

COPY src/ src/
# 3. Tell .NET to compile the binaries for the target architecture
RUN dotnet publish "src/Spim.Web/Spim.Web.csproj" -c Release -o /app/publish -a $TARGETARCH /p:UseAppHost=false

# 4. Use the TARGET architecture for the final, lightweight runtime image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Spim.Web.dll"]
