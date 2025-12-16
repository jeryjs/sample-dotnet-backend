# Multi-stage Dockerfile for .NET 8 Backend API

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY backend-api.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Copy configuration and data files
COPY appsettings.json .
COPY appsettings.Development.json .

# Copy JSON data files from parent directory if they exist (uncomment if needed)
# COPY --chown=appuser:appuser ../all_patients_data_f.json /data/
# COPY --chown=appuser:appuser ../getActiveAncillaryUsers.json /data/
# COPY --chown=appuser:appuser ../getActiveContactUsers.json /data/

# Set ownership
RUN chown -R appuser:appuser /app && \
    mkdir -p /data && \
    chown -R appuser:appuser /data

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "backend-api.dll"]
