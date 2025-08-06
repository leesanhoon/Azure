# Use the official ASP.NET Core runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/EnterpriseAuth.API/EnterpriseAuth.API.csproj", "src/EnterpriseAuth.API/"]
COPY ["src/EnterpriseAuth.Application/EnterpriseAuth.Application.csproj", "src/EnterpriseAuth.Application/"]
COPY ["src/EnterpriseAuth.Domain/EnterpriseAuth.Domain.csproj", "src/EnterpriseAuth.Domain/"]
COPY ["src/EnterpriseAuth.Infrastructure/EnterpriseAuth.Infrastructure.csproj", "src/EnterpriseAuth.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/EnterpriseAuth.API/EnterpriseAuth.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/EnterpriseAuth.API"
RUN dotnet build "EnterpriseAuth.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "EnterpriseAuth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Create directory for logs
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Set the entry point
ENTRYPOINT ["dotnet", "EnterpriseAuth.API.dll"]