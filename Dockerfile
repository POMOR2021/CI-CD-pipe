# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["WebApplication27.csproj", "./"]
RUN dotnet restore "WebApplication27.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "WebApplication27.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WebApplication27.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Create uploads directory for local storage
RUN mkdir -p /app/wwwroot/uploads && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WebApplication27.dll"]
