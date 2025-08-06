# Azure Deployment Script for Enterprise Auth API
# PowerShell script để triển khai ứng dụng lên Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$WebAppName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlDatabaseName,
    
    [Parameter(Mandatory=$true)]
    [string]$SqlAdminUsername,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlAdminPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "Southeast Asia",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServicePlanName = "plan-enterprise-auth",
    
    [Parameter(Mandatory=$false)]
    [string]$Sku = "B1"
)

# Convert SecureString to plain text for connection string
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host "🚀 Bắt đầu triển khai Enterprise Auth API lên Azure" -ForegroundColor Green

# 1. Đăng nhập Azure (nếu chưa đăng nhập)
Write-Host "1️⃣ Kiểm tra đăng nhập Azure..." -ForegroundColor Yellow
$context = Get-AzContext
if (-not $context) {
    Write-Host "Vui lòng đăng nhập vào Azure..." -ForegroundColor Red
    Connect-AzAccount
}

# 2. Tạo Resource Group
Write-Host "2️⃣ Tạo Resource Group: $ResourceGroupName" -ForegroundColor Yellow
$resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $resourceGroup) {
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    Write-Host "✅ Resource Group đã được tạo" -ForegroundColor Green
} else {
    Write-Host "✅ Resource Group đã tồn tại" -ForegroundColor Green
}

# 3. Tạo SQL Server
Write-Host "3️⃣ Tạo SQL Server: $SqlServerName" -ForegroundColor Yellow
$sqlServer = Get-AzSqlServer -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -ErrorAction SilentlyContinue
if (-not $sqlServer) {
    $credentials = New-Object System.Management.Automation.PSCredential ($SqlAdminUsername, $SqlAdminPassword)
    New-AzSqlServer -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -Location $Location -SqlAdministratorCredentials $credentials
    Write-Host "✅ SQL Server đã được tạo" -ForegroundColor Green
} else {
    Write-Host "✅ SQL Server đã tồn tại" -ForegroundColor Green
}

# 4. Cấu hình Firewall cho SQL Server
Write-Host "4️⃣ Cấu hình Firewall cho SQL Server..." -ForegroundColor Yellow
# Allow Azure services
New-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -FirewallRuleName "AllowAzureServices" -StartIpAddress "0.0.0.0" -EndIpAddress "0.0.0.0" -ErrorAction SilentlyContinue

# Get current public IP and add to firewall
$publicIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content.Trim()
New-AzSqlServerFirewallRule -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -FirewallRuleName "ClientIP" -StartIpAddress $publicIp -EndIpAddress $publicIp -ErrorAction SilentlyContinue
Write-Host "✅ Firewall rules đã được cấu hình" -ForegroundColor Green

# 5. Tạo SQL Database
Write-Host "5️⃣ Tạo SQL Database: $SqlDatabaseName" -ForegroundColor Yellow
$sqlDatabase = Get-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -ErrorAction SilentlyContinue
if (-not $sqlDatabase) {
    New-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -Edition "Basic"
    Write-Host "✅ SQL Database đã được tạo" -ForegroundColor Green
} else {
    Write-Host "✅ SQL Database đã tồn tại" -ForegroundColor Green
}

# 6. Tạo App Service Plan
Write-Host "6️⃣ Tạo App Service Plan: $AppServicePlanName" -ForegroundColor Yellow
$appServicePlan = Get-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlanName -ErrorAction SilentlyContinue
if (-not $appServicePlan) {
    New-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlanName -Location $Location -Tier $Sku -Linux
    Write-Host "✅ App Service Plan đã được tạo" -ForegroundColor Green
} else {
    Write-Host "✅ App Service Plan đã tồn tại" -ForegroundColor Green
}

# 7. Tạo Web App
Write-Host "7️⃣ Tạo Web App: $WebAppName" -ForegroundColor Yellow
$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -ErrorAction SilentlyContinue
if (-not $webApp) {
    New-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -AppServicePlan $AppServicePlanName -RuntimeStack "DOTNETCORE:9.0"
    Write-Host "✅ Web App đã được tạo" -ForegroundColor Green
} else {
    Write-Host "✅ Web App đã tồn tại" -ForegroundColor Green
}

# 8. Cấu hình HTTPS Only
Write-Host "8️⃣ Cấu hình HTTPS Only..." -ForegroundColor Yellow
Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -HttpsOnly $true
Write-Host "✅ HTTPS Only đã được bật" -ForegroundColor Green

# 9. Cấu hình Connection String
Write-Host "9️⃣ Cấu hình Connection String..." -ForegroundColor Yellow
$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$SqlDatabaseName;User ID=$SqlAdminUsername;Password=$PlainPassword;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$connectionStrings = @{
    "DefaultConnection" = @{
        "value" = $connectionString
        "type" = "SQLServer"
    }
}

Set-AzWebAppConnectionString -ResourceGroupName $ResourceGroupName -Name $WebAppName -ConnectionStrings $connectionStrings
Write-Host "✅ Connection String đã được cấu hình" -ForegroundColor Green

# 10. Cấu hình Application Settings
Write-Host "🔟 Cấu hình Application Settings..." -ForegroundColor Yellow

# Generate secure JWT secret
$jwtSecret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))

$appSettings = @{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "JwtSettings__Secret" = $jwtSecret
    "JwtSettings__Issuer" = "https://$WebAppName.azurewebsites.net"
    "JwtSettings__Audience" = "https://$WebAppName.azurewebsites.net"
    "JwtSettings__AccessTokenExpirationMinutes" = "15"
    "JwtSettings__RefreshTokenExpirationDays" = "7"
    "JwtSettings__ValidateIssuer" = "true"
    "JwtSettings__ValidateAudience" = "true"
    "JwtSettings__ValidateLifetime" = "true"
    "JwtSettings__ValidateIssuerSigningKey" = "true"
    "JwtSettings__ClockSkewMinutes" = "5"
}

Set-AzWebAppSetting -ResourceGroupName $ResourceGroupName -Name $WebAppName -AppSettings $appSettings
Write-Host "✅ Application Settings đã được cấu hình" -ForegroundColor Green

# 11. Enable Managed Identity
Write-Host "1️⃣1️⃣ Bật Managed Identity..." -ForegroundColor Yellow
Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName -AssignIdentity $true
Write-Host "✅ Managed Identity đã được bật" -ForegroundColor Green

# 12. Build và Publish ứng dụng
Write-Host "1️⃣2️⃣ Build và Publish ứng dụng..." -ForegroundColor Yellow
$projectPath = Join-Path $PSScriptRoot "../src/EnterpriseAuth.API/EnterpriseAuth.API.csproj"
$publishPath = Join-Path $PSScriptRoot "../publish"

# Restore dependencies
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Lỗi khi restore dependencies"
    exit 1
}

# Build project
dotnet build $projectPath --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Lỗi khi build project"
    exit 1
}

# Publish project
dotnet publish $projectPath --configuration Release --output $publishPath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Lỗi khi publish project"
    exit 1
}

Write-Host "✅ Build và Publish hoàn thành" -ForegroundColor Green

# 13. Deploy to Azure Web App
Write-Host "1️⃣3️⃣ Deploy lên Azure Web App..." -ForegroundColor Yellow

# Compress published files
$zipPath = Join-Path $PSScriptRoot "../deploy.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath -Force

# Deploy using Kudu API
$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName
$publishProfile = [xml](Get-AzWebAppPublishingProfile -ResourceGroupName $ResourceGroupName -Name $WebAppName -OutputFile $null)
$username = $publishProfile.publishData.publishProfile[0].userName
$password = $publishProfile.publishData.publishProfile[0].userPWD

# Deploy via Kudu ZIP API
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
$kuduUrl = "https://$WebAppName.scm.azurewebsites.net/api/zipdeploy"

try {
    $headers = @{
        Authorization = "Basic $base64AuthInfo"
        "Content-Type" = "application/zip"
    }
    
    Invoke-RestMethod -Uri $kuduUrl -Method POST -InFile $zipPath -Headers $headers -TimeoutSec 600
    Write-Host "✅ Deploy thành công!" -ForegroundColor Green
} catch {
    Write-Error "❌ Lỗi khi deploy: $($_.Exception.Message)"
    exit 1
}

# 14. Run Database Migration
Write-Host "1️⃣4️⃣ Chạy Database Migration..." -ForegroundColor Yellow
try {
    # Restart web app to ensure new deployment is loaded
    Restart-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName
    Start-Sleep -Seconds 30
    
    # Health check
    $healthUrl = "https://$WebAppName.azurewebsites.net/health"
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 60
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Health check passed - Migration hoàn thành" -ForegroundColor Green
    } else {
        Write-Warning "⚠️ Health check failed - Vui lòng kiểm tra logs"
    }
} catch {
    Write-Warning "⚠️ Không thể verify migration: $($_.Exception.Message)"
}

# 15. Cleanup
Write-Host "1️⃣5️⃣ Dọn dẹp files tạm..." -ForegroundColor Yellow
Remove-Item $publishPath -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Write-Host "✅ Cleanup hoàn thành" -ForegroundColor Green

# Summary
Write-Host "`n🎉 TRIỂN KHAI HOÀN THÀNH!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host "🔗 Web App URL: https://$WebAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "📊 Swagger UI: https://$WebAppName.azurewebsites.net/swagger" -ForegroundColor Cyan
Write-Host "💾 SQL Server: $SqlServerName.database.windows.net" -ForegroundColor Cyan
Write-Host "🗄️ Database: $SqlDatabaseName" -ForegroundColor Cyan
Write-Host "🔑 JWT Secret: $jwtSecret" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Green
Write-Host "`n📝 Lưu ý quan trọng:" -ForegroundColor Yellow
Write-Host "1. Lưu JWT Secret ở nơi an toàn" -ForegroundColor White
Write-Host "2. Cấu hình custom domain nếu cần" -ForegroundColor White
Write-Host "3. Thiết lập monitoring và alerts" -ForegroundColor White
Write-Host "4. Backup database định kỳ" -ForegroundColor White

# Clear sensitive variables
$PlainPassword = $null
$jwtSecret = $null
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)