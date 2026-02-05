# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


#ADDITIONAL DEPENDENCY
# Switch to root to install dependencies
USER root

# Update and install dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    apt-transport-https \
    ca-certificates \
    libgdiplus

# Download the wkhtmltox package
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb

# Install the wkhtmltox package and fix missing dependencies
RUN dpkg -i wkhtmltox_0.12.6.1-3.bookworm_amd64.deb || true && \
    apt-get -f install -y && \
    apt-get install -y --no-install-recommends wkhtmltox

# Clean up to reduce image size
RUN apt-get clean && rm -rf /var/lib/apt/lists/* wkhtmltox_0.12.6.1-3.bookworm_amd64.deb


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HTPDF.csproj", "."]
RUN dotnet restore "./HTPDF.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./HTPDF.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HTPDF.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HTPDF.dll"]