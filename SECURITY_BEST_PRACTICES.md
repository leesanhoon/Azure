# Security Best Practices cho Azure Deployment

## 🔒 Tổng quan Bảo mật

Hướng dẫn chi tiết về các best practices bảo mật khi triển khai Enterprise Auth API lên Azure, đảm bảo ứng dụng được bảo vệ tối ưu.

## 🛡️ 1. Azure SQL Database Security

### 1.1 Authentication & Authorization

```bash
# Sử dụng Azure AD Authentication thay vì SQL Authentication
az sql server ad-admin set \
  --resource-group rg-enterprise-auth \
  --server-name your-sql-server \
  --display-name "SQL Admin Group" \
  --object-id "your-ad-group-object-id"

# Tạo contained database user
# Trong SQL Server Management Studio hoặc Azure Data Studio:
CREATE USER [your-enterprise-auth-api] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your-enterprise-auth-api];
ALTER ROLE db_datawriter ADD MEMBER [your-enterprise-auth-api];
ALTER ROLE db_ddladmin ADD MEMBER [your-enterprise-auth-api];
```

### 1.2 Network Security

```bash
# Disable public network access
az sql server update \
  --resource-group rg-enterprise-auth \
  --name your-sql-server \
  --set publicNetworkAccess="Disabled"

# Tạo Private Endpoint
az network private-endpoint create \
  --resource-group rg-enterprise-auth \
  --name pe-sql-server \
  --vnet-name vnet-enterprise-auth \
  --subnet subnet-database \
  --private-connection-resource-id "/subscriptions/{subscription-id}/resourceGroups/rg-enterprise-auth/providers/Microsoft.Sql/servers/your-sql-server" \
  --group-ids sqlServer \
  --connection-name sql-connection
```

### 1.3 Data Encryption

```bash
# Enable Transparent Data Encryption (TDE)
az sql db tde set \
  --resource-group rg-enterprise-auth \
  --server your-sql-server \
  --database EnterpriseAuthDb \
  --status Enabled

# Enable Advanced Threat Protection
az sql db threat-policy update \
  --resource-group rg-enterprise-auth \
  --server your-sql-server \
  --database EnterpriseAuthDb \
  --state Enabled \
  --storage-account your-storage-account \
  --storage-endpoint https://yourstorageaccount.blob.core.windows.net \
  --storage-account-access-key "your-storage-key"
```

## 🔐 2. Azure Key Vault Integration

### 2.1 Tạo và Cấu hình Key Vault

```bash
# Tạo Key Vault
az keyvault create \
  --resource-group rg-enterprise-auth \
  --name kv-enterprise-auth-prod \
  --location "Southeast Asia" \
  --enabled-for-disk-encryption true \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true

# Cấu hình access policies
az keyvault set-policy \
  --name kv-enterprise-auth-prod \
  --object-id $(az webapp identity show --resource-group rg-enterprise-auth --name your-enterprise-auth-api --query principalId --output tsv) \
  --secret-permissions get list

# Thêm secrets
az keyvault secret set \
  --vault-name kv-enterprise-auth-prod \
  --name "SqlConnectionString" \
  --value "Server=tcp:your-sql-server.database.windows.net,1433;Initial Catalog=EnterpriseAuthDb;Authentication=Active Directory Managed Identity;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
  --vault-name kv-enterprise-auth-prod \
  --name "JwtSecret" \
  --value "$(openssl rand -base64 64 | tr -d '=+/' | cut -c1-64)"
```

### 2.2 Cập nhật Application Settings

```bash
# Cập nhật Web App để sử dụng Key Vault references
az webapp config appsettings set \
  --resource-group rg-enterprise-auth \
  --name your-enterprise-auth-api \
  --settings \
    ConnectionStrings__DefaultConnection="@Microsoft.KeyVault(VaultName=kv-enterprise-auth-prod;SecretName=SqlConnectionString)" \
    JwtSettings__Secret="@Microsoft.KeyVault(VaultName=kv-enterprise-auth-prod;SecretName=JwtSecret)"
```

## 🌐 3. Web App Security

### 3.1 Security Headers Implementation

Cập nhật [`src/EnterpriseAuth.API/Program.cs`](src/EnterpriseAuth.API/Program.cs:40):

```csharp
// Enhanced security headers middleware
app.Use(async (context, next) =>
{
    // Security headers
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    // Content Security Policy
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
              "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
              "font-src 'self' https://fonts.gstatic.com; " +
              "img-src 'self' data: https:; " +
              "connect-src 'self'; " +
              "frame-ancestors 'none';";

    context.Response.Headers.Add("Content-Security-Policy", csp);

    // HSTS (HTTP Strict Transport Security)
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    }

    await next();
});
```

### 3.2 Rate Limiting

Tạo [`src/EnterpriseAuth.API/Middleware/RateLimitingMiddleware.cs`](src/EnterpriseAuth.API/Middleware/RateLimitingMiddleware.cs):

```csharp
using System.Collections.Concurrent;
using System.Net;

namespace EnterpriseAuth.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, ClientStats> _clients = new();

        public RateLimitingMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var clientStats = _clients.GetOrAdd(clientId, _ => new ClientStats());

            // Rate limiting logic for authentication endpoints
            if (context.Request.Path.StartsWithSegments("/api/auth"))
            {
                if (!clientStats.AllowRequest())
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
                    return;
                }
            }

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // Use IP address and User-Agent for client identification
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            return $"{ip}:{userAgent.GetHashCode()}";
        }
    }

    public class ClientStats
    {
        private readonly List<DateTime> _requests = new();
        private readonly object _lock = new();
        private static readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
        private static readonly int _maxRequests = 10;

        public bool AllowRequest()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Remove old requests outside the time window
                _requests.RemoveAll(r => now - r > _timeWindow);

                // Check if we can allow this request
                if (_requests.Count < _maxRequests)
                {
                    _requests.Add(now);
                    return true;
                }

                return false;
            }
        }
    }
}
```

### 3.3 Input Validation và Sanitization

Cập nhật [`src/EnterpriseAuth.API/Extensions/ServiceExtensions.cs`](src/EnterpriseAuth.API/Extensions/ServiceExtensions.cs):

```csharp
public static IServiceCollection AddSecurityValidation(this IServiceCollection services)
{
    // Add Antiforgery
    services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "__Host-X-XSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Configure cookie policy
    services.Configure<CookiePolicyOptions>(options =>
    {
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.Strict;
        options.Secure = CookieSecurePolicy.Always;
    });

    return services;
}
```

## 🔍 4. Monitoring và Logging Security

### 4.1 Application Insights Security Events

```csharp
// Trong AuthController, thêm security logging
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Log security event
            _logger.LogInformation("Login attempt for user {Email} from IP {IP}",
                request.Email, HttpContext.Connection.RemoteIpAddress);

            var result = await _authService.LoginAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Successful login for user {Email}", request.Email);
            }
            else
            {
                _logger.LogWarning("Failed login attempt for user {Email} from IP {IP}. Reason: {Reason}",
                    request.Email, HttpContext.Connection.RemoteIpAddress, result.Message);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Email} from IP {IP}",
                request.Email, HttpContext.Connection.RemoteIpAddress);
            throw;
        }
    }
}
```

### 4.2 Azure Monitor Alerts

```bash
# Tạo action group cho security alerts
az monitor action-group create \
  --resource-group rg-enterprise-auth \
  --name ag-security-alerts \
  --short-name "SecAlert" \
  --email-receiver "Security Team" security@company.com

# Alert cho failed logins
az monitor metrics alert create \
  --resource-group rg-enterprise-auth \
  --name "Multiple Failed Logins" \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-enterprise-auth/providers/Microsoft.Web/sites/your-enterprise-auth-api" \
  --condition "count customMetrics/FailedLogins > 10" \
  --window-size "5m" \
  --evaluation-frequency "1m" \
  --action ag-security-alerts

# Alert cho high error rate
az monitor metrics alert create \
  --resource-group rg-enterprise-auth \
  --name "High Error Rate" \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-enterprise-auth/providers/Microsoft.Web/sites/your-enterprise-auth-api" \
  --condition "avg customMetrics/ErrorRate > 5" \
  --window-size "5m" \
  --evaluation-frequency "1m" \
  --action ag-security-alerts
```

## 🚨 5. Incident Response

### 5.1 Security Incident Playbook

1. **Phát hiện sự cố:**

   - Monitor alerts từ Application Insights
   - Kiểm tra logs bất thường
   - Reports từ users hoặc security scans

2. **Immediate Response:**

   ```bash
   # Disable compromised user accounts
   # Scale down app if under attack
   az webapp update --resource-group rg-enterprise-auth --name your-enterprise-auth-api --number-of-workers 1

   # Enable additional logging
   az webapp log config --resource-group rg-enterprise-auth --name your-enterprise-auth-api --application-logging filesystem --level verbose
   ```

3. **Investigation:**

   - Export logs từ Application Insights
   - Kiểm tra database audit logs
   - Review access patterns

4. **Recovery:**
   - Patch vulnerabilities
   - Update secrets
   - Scale app back up

### 5.2 Backup và Recovery

```bash
# Database backup
az sql db export \
  --resource-group rg-enterprise-auth \
  --server your-sql-server \
  --name EnterpriseAuthDb \
  --storage-key-type StorageAccessKey \
  --storage-key "your-storage-key" \
  --storage-uri "https://yourstorageaccount.blob.core.windows.net/backups/enterpriseauth-$(date +%Y%m%d).bacpac" \
  --admin-user "your-admin-user" \
  --admin-password "your-admin-password"

# App configuration backup
az webapp config show --resource-group rg-enterprise-auth --name your-enterprise-auth-api > app-config-backup.json
```

## 🔧 6. Security Testing

### 6.1 Automated Security Scanning

Thêm vào GitHub Actions workflow:

```yaml
- name: Run Security Scan
  uses: securecodewarrior/github-action-add-sarif@v1
  with:
    sarif-file: "security-scan-results.sarif"

- name: OWASP ZAP Baseline Scan
  uses: zaproxy/action-baseline@v0.7.0
  with:
    target: "https://your-enterprise-auth-api.azurewebsites.net"
    rules_file_name: ".zap/rules.tsv"

- name: Run Dependency Check
  uses: dependency-check/Dependency-Check_Action@main
  with:
    project: "Enterprise Auth API"
    path: "."
    format: "ALL"
```

### 6.2 Penetration Testing Checklist

- [ ] SQL Injection testing
- [ ] Cross-Site Scripting (XSS) testing
- [ ] Cross-Site Request Forgery (CSRF) testing
- [ ] Authentication bypass testing
- [ ] Authorization testing
- [ ] Input validation testing
- [ ] Session management testing
- [ ] Error handling testing

## 📋 7. Security Compliance

### 7.1 GDPR Compliance

```csharp
// Data retention policy implementation
public class DataRetentionService
{
    public async Task CleanupExpiredDataAsync()
    {
        // Delete expired refresh tokens
        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiryDate < DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);

        // Anonymize old user data (if required)
        var oldUsers = await _context.Users
            .Where(u => u.LastLoginDate < DateTime.UtcNow.AddYears(-2) && !u.IsActive)
            .ToListAsync();

        foreach (var user in oldUsers)
        {
            user.Email = $"anonymized-{user.Id}@deleted.local";
            user.FirstName = "Anonymized";
            user.LastName = "User";
            user.PhoneNumber = null;
        }

        await _context.SaveChangesAsync();
    }
}
```

### 7.2 Audit Logging

```csharp
public class AuditLog : BaseEntity
{
    public string UserId { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string IPAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; }
}

// Audit middleware
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Log all API calls for security audit
        var auditLog = new AuditLog
        {
            UserId = context.User.Identity?.Name,
            Action = context.Request.Method,
            Resource = context.Request.Path,
            IPAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers["User-Agent"],
            Timestamp = DateTime.UtcNow
        };

        // Save audit log
        await _auditService.LogAsync(auditLog);

        await _next(context);
    }
}
```

## ⚡ 8. Security Automation

### 8.1 Automated Security Updates

```yaml
# .github/workflows/security-updates.yml
name: Security Updates

on:
  schedule:
    - cron: "0 2 * * 1" # Weekly on Monday at 2 AM
  workflow_dispatch:

jobs:
  security-updates:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: "9.0.x"

      - name: Update NuGet packages
        run: |
          dotnet list package --outdated --include-transitive
          dotnet add package Microsoft.EntityFrameworkCore.SqlServer
          dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

      - name: Run security audit
        run: dotnet list package --vulnerable --include-transitive

      - name: Create Pull Request
        if: success()
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: "Security updates: Update vulnerable packages"
          title: "Security Updates"
          body: "Automated security updates for vulnerable packages"
```

### 8.2 Security Monitoring Script

```bash
#!/bin/bash
# security-monitor.sh

# Check for failed logins
FAILED_LOGINS=$(az monitor metrics list \
  --resource "/subscriptions/{subscription-id}/resourceGroups/rg-enterprise-auth/providers/Microsoft.Web/sites/your-enterprise-auth-api" \
  --metric "Http4xx" \
  --interval "1h" \
  --query "value[0].timeseries[0].data[-1].total" -o tsv)

if [ "$FAILED_LOGINS" -gt 100 ]; then
  echo "HIGH ALERT: Unusual number of failed requests detected: $FAILED_LOGINS"
  # Send alert to security team
fi

# Check SSL certificate expiration
CERT_EXPIRY=$(openssl s_client -connect your-enterprise-auth-api.azurewebsites.net:443 -servername your-enterprise-auth-api.azurewebsites.net </dev/null 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)
CERT_EXPIRY_TIMESTAMP=$(date -d "$CERT_EXPIRY" +%s)
CURRENT_TIMESTAMP=$(date +%s)
DAYS_UNTIL_EXPIRY=$(( ($CERT_EXPIRY_TIMESTAMP - $CURRENT_TIMESTAMP) / 86400 ))

if [ "$DAYS_UNTIL_EXPIRY" -lt 30 ]; then
  echo "WARNING: SSL certificate expires in $DAYS_UNTIL_EXPIRY days"
fi
```

## 📚 Resources

- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)

---

**Lưu ý quan trọng**: Bảo mật là một quá trình liên tục. Thường xuyên review và cập nhật các biện pháp bảo mật theo các threat landscape mới nhất.
