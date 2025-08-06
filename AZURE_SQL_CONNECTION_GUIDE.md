# Hướng dẫn kết nối Azure SQL Database

## Tổng quan

Dự án EnterpriseAuth đã được cấu hình để kết nối với Azure SQL Database sử dụng Active Directory Default authentication.

## Các thay đổi đã thực hiện

### 1. Cập nhật Connection String

- **File**: `src/EnterpriseAuth.API/appsettings.Production.json`
- **Connection String**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:netcoreazureninh.database.windows.net,1433;Initial Catalog=netcoreazure;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;"
  }
}
```

### 2. Thêm Azure Identity Package

- **File**: `src/EnterpriseAuth.Infrastructure/EnterpriseAuth.Infrastructure.csproj`
- **Package**: `Azure.Identity` version 1.13.1

### 3. Cấu hình DbContext

- **File**: `src/EnterpriseAuth.API/Extensions/ServiceExtensions.cs`
- **Thay đổi**: Đã cấu hình để hỗ trợ Azure SQL với Active Directory authentication

### 4. Database Schema Script

- **File**: `azure-database-script.sql`
- **Mô tả**: Script SQL để tạo database schema và seed data trên Azure SQL

## Yêu cầu triển khai

### 1. Azure Active Directory Configuration

- Azure SQL Database cần được cấu hình với Azure AD authentication
- Application hoặc Managed Identity cần có quyền truy cập database
- Database user cần được tạo cho Azure AD identity

### 2. Firewall Rules

- Azure SQL Server firewall cần cho phép kết nối từ:
  - Azure services (nếu deploy trên Azure)
  - IP addresses của development/production environment

### 3. Database Setup

1. Chạy script `azure-database-script.sql` trên Azure SQL Database
2. Tạo Azure AD user trong database:

```sql
CREATE USER [your-app-name] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your-app-name];
ALTER ROLE db_datawriter ADD MEMBER [your-app-name];
ALTER ROLE db_ddladmin ADD MEMBER [your-app-name];
```

## Troubleshooting

### Connection Timeout Issues

- Kiểm tra firewall rules trên Azure SQL Server
- Kiểm tra network connectivity
- Tăng connection timeout trong connection string

### Authentication Issues

- Đảm bảo Azure AD đã được cấu hình đúng
- Kiểm tra Managed Identity hoặc Service Principal permissions
- Verify Azure AD user exists trong database

### Local Development

Để test locally, bạn cần:

1. Đăng nhập Azure CLI: `az login`
2. Hoặc cấu hình Service Principal credentials
3. Hoặc sử dụng Visual Studio/VS Code Azure extension

## Commands hữu ích

### Tạo migration mới

```bash
dotnet ef migrations add MigrationName --project src/EnterpriseAuth.Infrastructure --startup-project src/EnterpriseAuth.API
```

### Generate SQL script

```bash
dotnet ef migrations script --project src/EnterpriseAuth.Infrastructure --startup-project src/EnterpriseAuth.API --output migration.sql
```

### Update database

```bash
dotnet ef database update --project src/EnterpriseAuth.Infrastructure --startup-project src/EnterpriseAuth.API
```

## Environment Variables (Production)

```bash
ASPNETCORE_ENVIRONMENT=Production
```

## Monitoring

- Sử dụng Azure SQL Analytics để monitor performance
- Enable Application Insights để track database connections
- Set up alerts cho connection failures

## Security Best Practices

1. Luôn sử dụng encryption (Encrypt=True)
2. Không store sensitive data trong connection strings
3. Sử dụng Managed Identity khi possible
4. Regularly rotate credentials
5. Monitor access logs
6. Implement proper error handling để avoid information disclosure
