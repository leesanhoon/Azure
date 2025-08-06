# Hướng dẫn Triển khai Enterprise Auth API lên Azure

## Tổng quan

Hướng dẫn chi tiết để kết nối và triển khai dự án **Enterprise Auth API** (.NET 9) lên Azure Web App và Azure SQL Database một cách an toàn và hiệu quả.

## 🏗️ Kiến trúc Dự án

Dự án sử dụng Clean Architecture với các layer sau:

- **API Layer**: [`EnterpriseAuth.API`](src/EnterpriseAuth.API/) - Controllers, Middleware, Extensions
- **Application Layer**: [`EnterpriseAuth.Application`](src/EnterpriseAuth.Application/) - Services, DTOs, Validators
- **Domain Layer**: [`EnterpriseAuth.Domain`](src/EnterpriseAuth.Domain/) - Entities, Interfaces, Exceptions
- **Infrastructure Layer**: [`EnterpriseAuth.Infrastructure`](src/EnterpriseAuth.Infrastructure/) - Data Access, Repositories

### Công nghệ sử dụng:

- **.NET 9** với ASP.NET Core
- **Entity Framework Core** với SQL Server
- **JWT Authentication**
- **Serilog** cho logging
- **FluentValidation** cho validation
- **Swagger** cho API documentation

## 🎯 Bước 1: Cấu hình Azure SQL Database

### 1.1 Lấy Connection String từ Azure Portal

1. Đăng nhập vào [Azure Portal](https://portal.azure.com)
2. Tìm Azure SQL Database đã tạo
3. Vào **Settings** > **Connection strings**
4. Copy connection string **ADO.NET (SQL authentication)**

Ví dụ connection string:

```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 1.2 Cấu hình Firewall Rules

Trong Azure SQL Server:

1. Vào **Security** > **Networking**
2. Thêm **Firewall rules**:
   - **Allow Azure services**: ON
   - **Add your client IP**: Thêm IP hiện tại
   - **Add Azure Web App IP** (sẽ có sau khi deploy)

### 1.3 Kiểm tra Connection

Test connection từ local:

```bash
# Cài đặt SQL Server command line tools
sqlcmd -S your-server.database.windows.net -d your-database -U your-username -P your-password -Q "SELECT 1"
```

## 🔧 Bước 2: Cấu hình Environment Variables

### 2.1 Tạo appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "#{AZURE_SQL_CONNECTION_STRING}#"
  },
  "JwtSettings": {
    "Secret": "#{JWT_SECRET}#",
    "Issuer": "#{JWT_ISSUER}#",
    "Audience": "#{JWT_AUDIENCE}#",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "ClockSkewMinutes": 5
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/home/LogFiles/Application/enterprise-auth-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  },
  "AllowedHosts": "*"
}
```

### 2.2 Danh sách Environment Variables cần thiết

| Variable Name                 | Mô tả                           | Ví dụ                              |
| ----------------------------- | ------------------------------- | ---------------------------------- |
| `AZURE_SQL_CONNECTION_STRING` | Connection string đến Azure SQL | Server=tcp:...                     |
| `JWT_SECRET`                  | Secret key cho JWT (≥256 bits)  | Random-Generated-256-Bit-Key       |
| `JWT_ISSUER`                  | JWT Issuer                      | https://your-app.azurewebsites.net |
| `JWT_AUDIENCE`                | JWT Audience                    | https://your-app.azurewebsites.net |
| `ASPNETCORE_ENVIRONMENT`      | Environment                     | Production                         |

## 🚀 Bước 3: Cấu hình Azure Web App

### 3.1 Tạo Azure Web App

```bash
# Tạo Resource Group (nếu chưa có)
az group create --name rg-enterprise-auth --location "Southeast Asia"

# Tạo App Service Plan
az appservice plan create \
  --name plan-enterprise-auth \
  --resource-group rg-enterprise-auth \
  --sku B1 \
  --is-linux

# Tạo Web App
az webapp create \
  --resource-group rg-enterprise-auth \
  --plan plan-enterprise-auth \
  --name your-enterprise-auth-api \
  --runtime "DOTNETCORE:9.0"
```

### 3.2 Cấu hình Application Settings

Trong Azure Portal > Web App > **Configuration** > **Application settings**:

```bash
# Connection String
az webapp config connection-string set \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;User ID=your-username;Password=your-password;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# JWT Settings
az webapp config appsettings set \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    JwtSettings__Secret="Your-Super-Secure-256-Bit-Secret-Key-Here-Change-This-In-Production" \
    JwtSettings__Issuer="https://your-enterprise-auth-api.azurewebsites.net" \
    JwtSettings__Audience="https://your-enterprise-auth-api.azurewebsites.net"
```

### 3.3 Cấu hình HTTPS và Custom Domain (tùy chọn)

```bash
# Force HTTPS
az webapp update \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --https-only true

# Cấu hình Custom Domain (nếu có)
az webapp config hostname add \
  --resource-group rg-enterprise-auth \
  --webapp-name your-enterprise-auth-api \
  --hostname api.yourdomain.com
```

## 🔐 Bước 4: Cấu hình Authentication & Authorization

### 4.1 Azure Active Directory Integration (tùy chọn)

Nếu muốn tích hợp với Azure AD:

```bash
# Tạo App Registration
az ad app create \
  --display-name "Enterprise Auth API" \
  --identifier-uris "https://your-enterprise-auth-api.azurewebsites.net"

# Lấy Application ID
az ad app list --display-name "Enterprise Auth API" --query "[].appId" --output tsv
```

### 4.2 Managed Identity cho Azure SQL

```bash
# Enable System Managed Identity
az webapp identity assign \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api

# Lấy Principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --query principalId --output tsv)

# Thêm quyền SQL Server
az sql server ad-admin set \
  --resource-group rg-enterprise-auth \
  --server-name your-sql-server \
  --display-name your-enterprise-auth-api \
  --object-id $PRINCIPAL_ID
```

### 4.3 Key Vault Integration (khuyến nghị)

```bash
# Tạo Key Vault
az keyvault create \
  --resource-group rg-enterprise-auth \
  --name kv-enterprise-auth \
  --location "Southeast Asia"

# Thêm secrets
az keyvault secret set \
  --vault-name kv-enterprise-auth \
  --name "JwtSecret" \
  --value "Your-Super-Secure-256-Bit-Secret-Key"

az keyvault secret set \
  --vault-name kv-enterprise-auth \
  --name "SqlConnectionString" \
  --value "Your-SQL-Connection-String"

# Cấp quyền cho Web App
az keyvault set-policy \
  --name kv-enterprise-auth \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get
```

## 📦 Bước 5: Deployment Configuration

### 5.1 Tạo GitHub Actions Workflow

```yaml
# .github/workflows/azure-deploy.yml
name: Deploy to Azure Web App

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_WEBAPP_NAME: your-enterprise-auth-api
  AZURE_WEBAPP_PACKAGE_PATH: "."
  DOTNET_VERSION: "9.0.x"

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET Core
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Set up dependency caching for faster builds
        uses: actions/cache@v3
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --verbosity normal --configuration Release

      - name: Publish
        run: dotnet publish src/EnterpriseAuth.API/EnterpriseAuth.API.csproj -c Release -o ${{env.DOTNET_ROOT}}/myapp

      - name: Upload artifact for deployment job
        uses: actions/upload-artifact@v3
        with:
          name: .net-app
          path: ${{env.DOTNET_ROOT}}/myapp

  deploy:
    runs-on: ubuntu-latest
    needs: build
    environment:
      name: "Production"
      url: ${{ steps.deploy-to-webapp.outputs.webapp-url }}

    steps:
      - name: Download artifact from build job
        uses: actions/download-artifact@v3
        with:
          name: .net-app

      - name: Deploy to Azure Web App
        id: deploy-to-webapp
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: .
```

### 5.2 Azure DevOps Pipeline (alternative)

```yaml
# azure-pipelines.yml
trigger:
  - main

variables:
  buildConfiguration: "Release"
  azureServiceConnection: "Azure-Service-Connection"
  webAppName: "your-enterprise-auth-api"

stages:
  - stage: Build
    displayName: Build stage
    jobs:
      - job: Build
        displayName: Build
        pool:
          vmImage: ubuntu-latest

        steps:
          - task: UseDotNet@2
            displayName: "Use .NET 9 SDK"
            inputs:
              packageType: "sdk"
              version: "9.0.x"

          - task: DotNetCoreCLI@2
            displayName: "Restore project dependencies"
            inputs:
              command: "restore"
              projects: "**/*.csproj"

          - task: DotNetCoreCLI@2
            displayName: "Build the project"
            inputs:
              command: "build"
              arguments: "--no-restore --configuration $(buildConfiguration)"

          - task: DotNetCoreCLI@2
            displayName: "Run tests"
            inputs:
              command: "test"
              projects: "**/*Tests.csproj"
              arguments: "--no-build --configuration $(buildConfiguration)"

          - task: DotNetCoreCLI@2
            displayName: "Publish the project"
            inputs:
              command: "publish"
              projects: "src/EnterpriseAuth.API/EnterpriseAuth.API.csproj"
              publishWebProjects: false
              arguments: "--no-build --configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)"
              zipAfterPublish: true

          - publish: $(Build.ArtifactStagingDirectory)
            artifact: drop

  - stage: Deploy
    displayName: Deploy stage
    dependsOn: Build
    condition: succeeded()
    jobs:
      - deployment: Deploy
        displayName: Deploy
        environment: "production"
        pool:
          vmImage: ubuntu-latest
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebApp@1
                  displayName: "Azure Web App Deploy"
                  inputs:
                    azureSubscription: $(azureServiceConnection)
                    appType: "webAppLinux"
                    appName: $(webAppName)
                    package: "$(Pipeline.Workspace)/drop/**/*.zip"
```

## 🗃️ Bước 6: Database Migration

### 6.1 Cập nhật Connection String cho Migration

Tạo file tạm thời [`appsettings.Migration.json`](src/EnterpriseAuth.API/appsettings.Migration.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;User ID=your-username;Password=your-password;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### 6.2 Chạy Migration

```bash
# Từ thư mục gốc dự án
cd src/EnterpriseAuth.API

# Add migration (nếu chưa có)
dotnet ef migrations add InitialCreate --project ../EnterpriseAuth.Infrastructure

# Update database trên Azure
dotnet ef database update --configuration Migration

# Xóa file migration tạm thời
rm appsettings.Migration.json
```

### 6.3 Seed Initial Data

Thêm script seed data trong [`Program.cs`](src/EnterpriseAuth.API/Program.cs:60):

```csharp
// Migrate and seed database in production
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed successfully");

        // Seed admin user if not exists
        await SeedAdminUser(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during database migration");
        throw;
    }
}
```

## 🔒 Bước 7: Security Best Practices

### 7.1 Cấu hình Security Headers

Thêm vào [`Program.cs`](src/EnterpriseAuth.API/Program.cs:44):

```csharp
// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

    await next();
});
```

### 7.2 Rate Limiting

```csharp
// Thêm vào ServiceExtensions.cs
public static IServiceCollection AddRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 2;
        });

        options.AddGlobalLimiter(PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                })));
    });

    return services;
}
```

### 7.3 Logging và Monitoring

Cấu hình Application Insights:

```bash
# Thêm Application Insights
az webapp config appsettings set \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --settings \
    APPINSIGHTS_INSTRUMENTATIONKEY="your-instrumentation-key" \
    ApplicationInsightsAgent_EXTENSION_VERSION="~3"
```

## 📋 Bước 8: Testing và Verification

### 8.1 Health Check Endpoints

Test các endpoints sau khi deploy:

```bash
# Health check
curl https://your-enterprise-auth-api.azurewebsites.net/health

# API documentation
curl https://your-enterprise-auth-api.azurewebsites.net/swagger

# Test login endpoint
curl -X POST https://your-enterprise-auth-api.azurewebsites.net/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@123456"}'
```

### 8.2 Load Testing

```bash
# Cài đặt Azure Load Testing CLI
az extension add --name load

# Tạo simple load test
az load test create \
  --resource-group rg-enterprise-auth \
  --name load-test-enterprise-auth \
  --test-file load-test.jmx
```

### 8.3 Monitoring Setup

```bash
# Tạo Action Group cho alerts
az monitor action-group create \
  --resource-group rg-enterprise-auth \
  --name ag-enterprise-auth \
  --short-name "EntAuth"

# Tạo alert rule cho high CPU
az monitor metrics alert create \
  --resource-group rg-enterprise-auth \
  --name "High CPU Alert" \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-enterprise-auth/providers/Microsoft.Web/sites/your-enterprise-auth-api" \
  --condition "avg Percentage CPU > 80" \
  --action ag-enterprise-auth
```

## 🚀 Deployment Checklist

### Pre-deployment:

- [ ] Azure SQL Database đã tạo và có firewall rules
- [ ] Connection string đã được test
- [ ] Azure Web App đã được tạo
- [ ] Environment variables đã được cấu hình
- [ ] SSL certificate đã được cấu hình
- [ ] Domain name đã được cấu hình (nếu có)

### During deployment:

- [ ] CI/CD pipeline đã chạy thành công
- [ ] Database migration đã hoàn thành
- [ ] Application logs không có errors
- [ ] Health check endpoints hoạt động

### Post-deployment:

- [ ] API endpoints hoạt động bình thường
- [ ] Authentication/Authorization hoạt động
- [ ] Logging và monitoring đã được setup
- [ ] Performance testing đã được thực hiện
- [ ] Security scan đã được thực hiện

## 🔧 Troubleshooting

### Các lỗi thường gặp:

1. **Connection timeout**: Kiểm tra firewall rules của Azure SQL
2. **JWT validation failed**: Kiểm tra JWT secret và issuer/audience
3. **Migration failed**: Kiểm tra connection string và permissions
4. **High memory usage**: Tăng App Service Plan hoặc optimize code
5. **Slow response time**: Cấu hình Application Insights để debug

### Useful commands:

```bash
# Xem logs của Web App
az webapp log tail --resource-group rg-enterprise-auth --name your-enterprise-auth-api

# Restart Web App
az webapp restart --resource-group rg-enterprise-auth --name your-enterprise-auth-api

# Scale up App Service Plan
az appservice plan update --resource-group rg-enterprise-auth --name plan-enterprise-auth --sku S1
```

## 📚 Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [.NET Core Deployment Guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/)
- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Lưu ý**: Thay thế tất cả placeholder values (`your-server`, `your-database`, etc.) bằng thông tin thực tế của Azure resources.
