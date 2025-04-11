# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Set the NuGet package cache directory to a Linux-friendly path
ENV NUGET_PACKAGES=/root/.nuget/packages
ENV NUGET_FALLBACK_PACKAGES=""
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV NUGET_FALLBACK_PACKAGES=

# Copy project files and restore dependencies
COPY *.csproj .

# Copy everything else and build
COPY . .
RUN dotnet nuget locals all --clear
RUN dotnet restore --packages /root/.nuget/packages --disable-parallel
RUN dotnet publish -c release -o /app --no-restore


# Use the ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy the built application from the build image
COPY --from=build /app .

# Expose the port your app runs on
EXPOSE 5000

# Environment variables (can be overridden in docker-compose)
ENV DOTNET_URL=http://+:5000
ENV KAFKA_URL=kafka:9092

# Run the application
ENTRYPOINT ["dotnet", "reservation.dll"]