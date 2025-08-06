# 🚀 Enterprise Auth API - Azure Deployment Guide

## 📋 Tổng quan

Hướng dẫn hoàn chỉnh để triển khai **Enterprise Auth API** (.NET 9) lên **Azure Web App** và **Azure SQL Database** một cách an toàn, hiệu quả và tuân thủ các best practices.

## 🎯 Mục tiêu

- ✅ Kết nối an toàn với Azure SQL Database
- ✅ Triển khai lên Azure Web App với CI/CD
- ✅ Cấu hình bảo mật enterprise-grade
- ✅ Monitoring và logging comprehensive
- ✅ Automation deployment process

## 📁 Cấu trúc Files được tạo

```
├── AZURE_DEPLOYMENT_GUIDE.md          # Hướng dẫn deployment chi tiết
├── SECURITY_BEST_PRACTICES.md         # Best practices bảo mật
├── src/EnterpriseAuth.API/
│   └── appsettings.Production.json    # Cấu hình production
├── .github/workflows/
│   └── azure-deploy.yml               # GitHub Actions CI/CD
└── scripts/
    ├── deploy-to-azure.ps1            # PowerShell deployment script
    └── deploy-to-azure.sh             # Bash deployment script
```

## 🚀 Quick Start

### Bước 1: Chuẩn bị môi trường

1. **Azure CLI**: Cài đặt và đăng nhập

```bash
# Cài đặt Azure CLI (macOS)
brew install azure-cli

# Đăng nhập
az login
```

2. **Tạo Azure Resources** (nếu chưa có):

```bash
# Sử dụng script tự động
./scripts/deploy-to-azure.sh \
  "rg-enterprise-auth" \
  "my-enterprise-auth-api" \
  "my-sql-server" \
  "EnterpriseAuthDb" \
  "sqladmin" \
  "MySecurePass@123"
```

### Bước 2: Cấu hình Environment Variables

Trong Azure Portal > Web App > Configuration:

| Variable                 | Value                                | Mô tả          |
| ------------------------ | ------------------------------------ | -------------- |
| `ASPNETCORE_ENVIRONMENT` | `Production`                         | Environment    |
| `JwtSettings__Secret`    | `[Auto-generated]`                   | JWT Secret Key |
| `JwtSettings__Issuer`    | `https://your-app.azurewebsites.net` | JWT Issuer     |
| `JwtSettings__Audience`  | `https://your-app.azurewebsites.net` | JWT Audience   |

### Bước 3: Setup CI/CD với GitHub Actions

1. **Lấy Publish Profile**:

```bash
az webapp deployment list-publishing-profiles \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --xml
```

2. **Thêm GitHub Secrets**:

   - `AZURE_WEBAPP_PUBLISH_PROFILE`: Publish profile từ bước 1
   - `AZURE_RESOURCE_GROUP`: Tên resource group

3. **Push code để trigger deployment**:

```bash
git add .
git commit -m "Setup Azure deployment"
git push origin main
```

## 📚 Tài liệu chi tiết

### 🔧 [AZURE_DEPLOYMENT_GUIDE.md](AZURE_DEPLOYMENT_GUIDE.md)

Hướng dẫn chi tiết về:

- Cấu hình Azure SQL Database
- Setup Azure Web App
- Environment variables management
- Database migration strategies
- CI/CD pipeline configuration
- Troubleshooting common issues

### 🛡️ [SECURITY_BEST_PRACTICES.md](SECURITY_BEST_PRACTICES.md)

Best practices bảo mật:

- Azure Key Vault integration
- Network security configuration
- Authentication & authorization
- Security headers implementation
- Monitoring & incident response
- Compliance guidelines

## 🔄 Deployment Options

### Option 1: Automated Script Deployment

```bash
# PowerShell (Windows)
.\scripts\deploy-to-azure.ps1 -ResourceGroupName "rg-enterprise-auth" -WebAppName "my-api" -SqlServerName "my-sql" -SqlDatabaseName "AuthDb" -SqlAdminUsername "admin" -SqlAdminPassword (ConvertTo-SecureString "Password123!" -AsPlainText -Force)

# Bash (Linux/macOS)
./scripts/deploy-to-azure.sh "rg-enterprise-auth" "my-api" "my-sql" "AuthDb" "admin" "Password123!"
```

### Option 2: GitHub Actions CI/CD

- Tự động trigger khi push lên `main` branch
- Build, test, và deploy tự động
- Health check sau deployment
- Rollback tự động nếu có lỗi

### Option 3: Manual Azure CLI

Theo hướng dẫn trong [`AZURE_DEPLOYMENT_GUIDE.md`](AZURE_DEPLOYMENT_GUIDE.md)

## 🔒 Security Features

### ✅ Implemented

- [x] HTTPS only với HSTS
- [x] Security headers (XSS, CSRF, etc.)
- [x] JWT authentication với secure secrets
- [x] Connection string encryption
- [x] SQL injection protection
- [x] Rate limiting cho auth endpoints
- [x] Audit logging
- [x] Input validation
- [x] Error handling không leak information

### 🔄 Recommended Additions

- [ ] Azure Key Vault integration
- [ ] Private endpoints cho SQL Database
- [ ] Web Application Firewall (WAF)
- [ ] DDoS protection
- [ ] Managed Identity cho database access
- [ ] Azure Security Center monitoring

## 📊 Monitoring & Health Checks

### Health Check Endpoints

- `/health` - Basic health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Application Insights

- Request tracking
- Dependency tracking
- Exception tracking
- Performance counters
- Custom telemetry

### Alerts Configuration

- High error rate (>5%)
- Failed authentication attempts (>10/min)
- High response time (>2s)
- Database connection failures

## 🔧 Configuration Management

### Development

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseAuthDb;Trusted_Connection=true;"
  }
}
```

### Production

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "#{AZURE_SQL_CONNECTION_STRING}#"
  },
  "JwtSettings": {
    "Secret": "#{JWT_SECRET}#"
  }
}
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Database Connection Timeout

```bash
# Check firewall rules
az sql server firewall-rule list --resource-group rg-enterprise-auth --server your-sql-server

# Add current IP
az sql server firewall-rule create --resource-group rg-enterprise-auth --server your-sql-server --name "CurrentIP" --start-ip-address "YOUR_IP" --end-ip-address "YOUR_IP"
```

#### 2. JWT Validation Failed

```bash
# Verify JWT settings
az webapp config appsettings list --resource-group rg-enterprise-auth --name your-webapp --query "[?name=='JwtSettings__Secret']"
```

#### 3. Migration Failed

```bash
# Check database permissions
az sql db show --resource-group rg-enterprise-auth --server your-sql-server --name your-database

# Run migration manually
dotnet ef database update --configuration Production
```

### Logs Access

```bash
# Stream logs
az webapp log tail --resource-group rg-enterprise-auth --name your-webapp

# Download logs
az webapp log download --resource-group rg-enterprise-auth --name your-webapp
```

## 📈 Performance Optimization

### App Service Plan Recommendations

- **Development**: B1 (Basic)
- **Staging**: S1 (Standard)
- **Production**: P1V2+ (Premium) with auto-scaling

### Database Performance

- **Development**: Basic (5 DTU)
- **Production**: Standard S2+ (50+ DTU)
- Consider Azure SQL Database Hyperscale for large datasets

### Caching Strategy

```csharp
// Add Redis cache (optional)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

## 🔄 Backup & Recovery

### Database Backup

```bash
# Automated backups are enabled by default
# Manual backup export
az sql db export --resource-group rg-enterprise-auth --server your-sql-server --name your-database --storage-key-type StorageAccessKey --storage-key "your-key" --storage-uri "https://storage.blob.core.windows.net/backups/backup.bacpac" --admin-user "admin" --admin-password "password"
```

### Application Backup

```bash
# Backup app configuration
az webapp config show --resource-group rg-enterprise-auth --name your-webapp > app-config-backup.json

# Backup app content (if needed)
az webapp deployment source config-zip --resource-group rg-enterprise-auth --name your-webapp --src backup.zip
```

## 📞 Support & Maintenance

### Regular Maintenance Tasks

- [ ] Security updates (monthly)
- [ ] Dependency updates (weekly)
- [ ] Performance monitoring (daily)
- [ ] Log review (weekly)
- [ ] Backup verification (monthly)

### Monitoring Dashboards

- Azure Portal Application Insights
- Azure Monitor Workbooks
- Custom Grafana dashboards (optional)

## 📝 Change Log

### Version 1.0 (Current)

- ✅ Initial Azure deployment setup
- ✅ Basic security configuration
- ✅ CI/CD pipeline
- ✅ Monitoring setup

### Planned Features

- 🔄 Advanced security features
- 🔄 Multi-region deployment
- 🔄 Disaster recovery setup
- 🔄 Advanced monitoring

## 🤝 Contributing

1. Fork repository
2. Create feature branch
3. Test deployment thoroughly
4. Update documentation
5. Submit pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**⚠️ Important Notes:**

- Always test deployment in staging environment first
- Keep secrets secure and rotate regularly
- Monitor application performance and security
- Follow Azure Well-Architected Framework principles
- Maintain documentation up to date

**🆘 Need Help?**

- Check [AZURE_DEPLOYMENT_GUIDE.md](AZURE_DEPLOYMENT_GUIDE.md) for detailed instructions
- Review [SECURITY_BEST_PRACTICES.md](SECURITY_BEST_PRACTICES.md) for security guidelines
- Open GitHub issue for deployment problems
- Contact Azure support for infrastructure issues
