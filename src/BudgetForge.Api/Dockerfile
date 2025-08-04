# Multi-stage build for optimized production image
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /app

# Copy csproj files and restore dependencies (layer caching optimization)
COPY src/BudgetForge.Domain/BudgetForge.Domain.csproj src/BudgetForge.Domain/
COPY src/BudgetForge.Application/BudgetForge.Application.csproj src/BudgetForge.Application/
COPY src/BudgetForge.Infrastructure/BudgetForge.Infrastructure.csproj src/BudgetForge.Infrastructure/
COPY src/BudgetForge.Api/BudgetForge.Api.csproj src/BudgetForge.Api/
COPY BudgetForge.sln .

# Restore dependencies with all transitive packages
RUN dotnet restore --verbosity normal

# Copy source code
COPY src/ src/

# Build and publish in one step (avoids analyzer issues)
RUN dotnet publish src/BudgetForge.Api/BudgetForge.Api.csproj \
    -c Release \
    -o /app/publish \
    --verbosity normal

# Stage 2: Runtime environment (much smaller)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install additional packages for production monitoring
RUN apk add --no-cache \
    curl \
    tzdata \
    && rm -rf /var/cache/apk/*

# Create non-root user for security
RUN addgroup -g 1001 -S appgroup \
    && adduser -u 1001 -S appuser -G appgroup

# Set working directory and ownership
WORKDIR /app
RUN chown appuser:appgroup /app

# Copy published application from build stage
COPY --from=build-env --chown=appuser:appgroup /app/publish .

# Switch to non-root user
USER appuser

# Configure ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Health check for Kubernetes
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "BudgetForge.Api.dll"]