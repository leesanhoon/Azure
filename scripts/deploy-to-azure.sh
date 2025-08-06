#!/bin/bash

# Azure Deployment Script for Enterprise Auth API
# Bash script để triển khai ứng dụng lên Azure

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${CYAN}$1${NC}"
}

print_step() {
    echo -e "${YELLOW}$1${NC}"
}

# Check if parameters are provided
if [ "$#" -lt 6 ]; then
    print_error "Usage: $0 <ResourceGroupName> <WebAppName> <SqlServerName> <SqlDatabaseName> <SqlAdminUsername> <SqlAdminPassword> [Location] [AppServicePlanName] [Sku]"
    echo "Example: $0 rg-enterprise-auth my-enterprise-auth-api my-sql-server EnterpriseAuthDb sqladmin MySecurePass@123 'Southeast Asia' plan-enterprise-auth B1"
    exit 1
fi

# Parameters
RESOURCE_GROUP_NAME=$1
WEB_APP_NAME=$2
SQL_SERVER_NAME=$3
SQL_DATABASE_NAME=$4
SQL_ADMIN_USERNAME=$5
SQL_ADMIN_PASSWORD=$6
LOCATION=${7:-"Southeast Asia"}
APP_SERVICE_PLAN_NAME=${8:-"plan-enterprise-auth"}
SKU=${9:-"B1"}

echo -e "${GREEN}🚀 Bắt đầu triển khai Enterprise Auth API lên Azure${NC}"

# 1. Check Azure CLI installation and login
print_step "1️⃣ Kiểm tra Azure CLI và đăng nhập..."
if ! command -v az &> /dev/null; then
    print_error "Azure CLI chưa được cài đặt. Vui lòng cài đặt Azure CLI trước."
    exit 1
fi

# Check if logged in
if ! az account show &> /dev/null; then
    print_warning "Vui lòng đăng nhập vào Azure..."
    az login
fi

print_status "Azure CLI đã sẵn sàng"

# 2. Create Resource Group
print_step "2️⃣ Tạo Resource Group: $RESOURCE_GROUP_NAME"
if az group show --name "$RESOURCE_GROUP_NAME" &> /dev/null; then
    print_status "Resource Group đã tồn tại"
else
    az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION"
    print_status "Resource Group đã được tạo"
fi

# 3. Create SQL Server
print_step "3️⃣ Tạo SQL Server: $SQL_SERVER_NAME"
if az sql server show --resource-group "$RESOURCE_GROUP_NAME" --name "$SQL_SERVER_NAME" &> /dev/null; then
    print_status "SQL Server đã tồn tại"
else
    az sql server create \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$SQL_SERVER_NAME" \
        --location "$LOCATION" \
        --admin-user "$SQL_ADMIN_USERNAME" \
        --admin-password "$SQL_ADMIN_PASSWORD"
    print_status "SQL Server đã được tạo"
fi

# 4. Configure SQL Server Firewall
print_step "4️⃣ Cấu hình Firewall cho SQL Server..."

# Allow Azure services
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --server "$SQL_SERVER_NAME" \
    --name "AllowAzureServices" \
    --start-ip-address "0.0.0.0" \
    --end-ip-address "0.0.0.0" \
    --output none 2>/dev/null || true

# Get current public IP and add to firewall
PUBLIC_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --server "$SQL_SERVER_NAME" \
    --name "ClientIP" \
    --start-ip-address "$PUBLIC_IP" \
    --end-ip-address "$PUBLIC_IP" \
    --output none 2>/dev/null || true

print_status "Firewall rules đã được cấu hình"

# 5. Create SQL Database
print_step "5️⃣ Tạo SQL Database: $SQL_DATABASE_NAME"
if az sql db show --resource-group "$RESOURCE_GROUP_NAME" --server "$SQL_SERVER_NAME" --name "$SQL_DATABASE_NAME" &> /dev/null; then
    print_status "SQL Database đã tồn tại"
else
    az sql db create \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --server "$SQL_SERVER_NAME" \
        --name "$SQL_DATABASE_NAME" \
        --edition "Basic"
    print_status "SQL Database đã được tạo"
fi

# 6. Create App Service Plan
print_step "6️⃣ Tạo App Service Plan: $APP_SERVICE_PLAN_NAME"
if az appservice plan show --resource-group "$RESOURCE_GROUP_NAME" --name "$APP_SERVICE_PLAN_NAME" &> /dev/null; then
    print_status "App Service Plan đã tồn tại"
else
    az appservice plan create \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$APP_SERVICE_PLAN_NAME" \
        --location "$LOCATION" \
        --sku "$SKU" \
        --is-linux
    print_status "App Service Plan đã được tạo"
fi

# 7. Create Web App
print_step "7️⃣ Tạo Web App: $WEB_APP_NAME"
if az webapp show --resource-group "$RESOURCE_GROUP_NAME" --name "$WEB_APP_NAME" &> /dev/null; then
    print_status "Web App đã tồn tại"
else
    az webapp create \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --plan "$APP_SERVICE_PLAN_NAME" \
        --name "$WEB_APP_NAME" \
        --runtime "DOTNETCORE:9.0"
    print_status "Web App đã được tạo"
fi

# 8. Configure HTTPS Only
print_step "8️⃣ Cấu hình HTTPS Only..."
az webapp update \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --https-only true
print_status "HTTPS Only đã được bật"

# 9. Configure Connection String
print_step "9️⃣ Cấu hình Connection String..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DATABASE_NAME};User ID=${SQL_ADMIN_USERNAME};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --connection-string-type "SQLServer" \
    --settings DefaultConnection="$CONNECTION_STRING"

print_status "Connection String đã được cấu hình"

# 10. Configure Application Settings
print_step "🔟 Cấu hình Application Settings..."

# Generate secure JWT secret
JWT_SECRET=$(openssl rand -base64 64 | tr -d "=+/" | cut -c1-64)

az webapp config appsettings set \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --settings \
        ASPNETCORE_ENVIRONMENT="Production" \
        JwtSettings__Secret="$JWT_SECRET" \
        JwtSettings__Issuer="https://${WEB_APP_NAME}.azurewebsites.net" \
        JwtSettings__Audience="https://${WEB_APP_NAME}.azurewebsites.net" \
        JwtSettings__AccessTokenExpirationMinutes="15" \
        JwtSettings__RefreshTokenExpirationDays="7" \
        JwtSettings__ValidateIssuer="true" \
        JwtSettings__ValidateAudience="true" \
        JwtSettings__ValidateLifetime="true" \
        JwtSettings__ValidateIssuerSigningKey="true" \
        JwtSettings__ClockSkewMinutes="5"

print_status "Application Settings đã được cấu hình"

# 11. Enable Managed Identity
print_step "1️⃣1️⃣ Bật Managed Identity..."
az webapp identity assign \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME"
print_status "Managed Identity đã được bật"

# 12. Build and Publish application
print_step "1️⃣2️⃣ Build và Publish ứng dụng..."

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/../src/EnterpriseAuth.API/EnterpriseAuth.API.csproj"
PUBLISH_PATH="$SCRIPT_DIR/../publish"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK chưa được cài đặt. Vui lòng cài đặt .NET 9 SDK."
    exit 1
fi

# Clean previous publish
rm -rf "$PUBLISH_PATH"

# Restore dependencies
print_info "Restore dependencies..."
dotnet restore "$PROJECT_PATH"

# Build project
print_info "Build project..."
dotnet build "$PROJECT_PATH" --configuration Release --no-restore

# Publish project
print_info "Publish project..."
dotnet publish "$PROJECT_PATH" --configuration Release --output "$PUBLISH_PATH" --no-build

print_status "Build và Publish hoàn thành"

# 13. Deploy to Azure Web App
print_step "1️⃣3️⃣ Deploy lên Azure Web App..."

# Create deployment package
ZIP_PATH="$SCRIPT_DIR/../deploy.zip"
rm -f "$ZIP_PATH"

# Create zip file
cd "$PUBLISH_PATH"
zip -r "$ZIP_PATH" . > /dev/null
cd - > /dev/null

# Deploy using az webapp deployment
az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --src "$ZIP_PATH"

print_status "Deploy thành công!"

# 14. Run Database Migration
print_step "1️⃣4️⃣ Kiểm tra ứng dụng và Migration..."

# Restart web app to ensure new deployment is loaded
az webapp restart --resource-group "$RESOURCE_GROUP_NAME" --name "$WEB_APP_NAME"

# Wait for app to start
print_info "Đợi ứng dụng khởi động..."
sleep 30

# Health check
HEALTH_URL="https://${WEB_APP_NAME}.azurewebsites.net/health"
if curl -f -s "$HEALTH_URL" > /dev/null; then
    print_status "Health check passed - Migration hoàn thành"
else
    print_warning "Health check failed - Vui lòng kiểm tra logs"
fi

# 15. Cleanup
print_step "1️⃣5️⃣ Dọn dẹp files tạm..."
rm -rf "$PUBLISH_PATH"
rm -f "$ZIP_PATH"
print_status "Cleanup hoàn thành"

# Summary
echo ""
echo -e "${GREEN}🎉 TRIỂN KHAI HOÀN THÀNH!${NC}"
echo "====================================="
echo -e "${CYAN}🔗 Web App URL: https://${WEB_APP_NAME}.azurewebsites.net${NC}"
echo -e "${CYAN}📊 Swagger UI: https://${WEB_APP_NAME}.azurewebsites.net/swagger${NC}"
echo -e "${CYAN}💾 SQL Server: ${SQL_SERVER_NAME}.database.windows.net${NC}"
echo -e "${CYAN}🗄️ Database: ${SQL_DATABASE_NAME}${NC}"
echo -e "${YELLOW}🔑 JWT Secret: ${JWT_SECRET}${NC}"
echo "====================================="
echo ""
echo -e "${YELLOW}📝 Lưu ý quan trọng:${NC}"
echo -e "${WHITE}1. Lưu JWT Secret ở nơi an toàn${NC}"
echo -e "${WHITE}2. Cấu hình custom domain nếu cần${NC}"
echo -e "${WHITE}3. Thiết lập monitoring và alerts${NC}"
echo -e "${WHITE}4. Backup database định kỳ${NC}"

print_status "Script hoàn thành thành công!"