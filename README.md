# Enterprise Authentication API

Một hệ thống xác thực enterprise-grade được xây dựng với .NET Core sử dụng Clean Architecture pattern, JWT authentication, và các best practices hiện đại.

## 🚀 Tính năng chính

- **Clean Architecture**: Tách biệt rõ ràng giữa các layer (Domain, Application, Infrastructure, Presentation)
- **JWT Authentication**: Bảo mật với Access Token và Refresh Token
- **Role-based Authorization**: Hệ thống phân quyền linh hoạt với Roles và Permissions
- **Password Hashing**: Mã hóa mật khẩu an toàn với BCrypt
- **Validation**: Input validation với FluentValidation
- **Logging**: Structured logging với Serilog
- **Exception Handling**: Global exception handling middleware
- **API Documentation**: Swagger/OpenAPI integration
- **Health Checks**: Monitoring và health endpoints
- **Database Support**: Entity Framework Core với SQL Server
- **CORS Policy**: Cấu hình CORS cho frontend integration

## 📁 Cấu trúc dự án

```
EnterpriseAuth/
├── src/
│   ├── EnterpriseAuth.API/              # Presentation Layer
│   │   ├── Controllers/                 # API Controllers
│   │   ├── Extensions/                  # Service Extensions
│   │   ├── Middleware/                  # Custom Middleware
│   │   └── Program.cs                   # Application Entry Point
│   ├── EnterpriseAuth.Application/      # Application Layer
│   │   ├── DTOs/                        # Data Transfer Objects
│   │   ├── Interfaces/                  # Application Interfaces
│   │   ├── Services/                    # Business Logic Services
│   │   └── Validators/                  # Input Validators
│   ├── EnterpriseAuth.Domain/           # Domain Layer
│   │   ├── Entities/                    # Domain Entities
│   │   ├── Interfaces/                  # Domain Interfaces
│   │   └── Exceptions/                  # Domain Exceptions
│   └── EnterpriseAuth.Infrastructure/   # Infrastructure Layer
│       ├── Data/                        # Database Context
│       ├── Configurations/              # Entity Configurations
│       ├── Repositories/                # Repository Implementations
│       └── Migrations/                  # Database Migrations
└── tests/
    ├── EnterpriseAuth.UnitTests/        # Unit Tests
    └── EnterpriseAuth.IntegrationTests/ # Integration Tests
```

## 🛠️ Công nghệ sử dụng

- **.NET 9.0**
- **Entity Framework Core 9.0**
- **SQL Server** (có thể thay đổi sang SQLite cho development)
- **JWT Bearer Authentication**
- **BCrypt.Net** cho password hashing
- **FluentValidation** cho input validation
- **Serilog** cho structured logging
- **Swagger/OpenAPI** cho API documentation
- **xUnit** cho unit testing

## 🚦 Bắt đầu nhanh

### Yêu cầu hệ thống

- .NET 9.0 SDK
- SQL Server hoặc SQL Server LocalDB
- Visual Studio 2022 hoặc VS Code

### Cài đặt

1. **Clone repository**

```bash
git clone https://github.com/yourusername/enterprise-auth.git
cd enterprise-auth
```

2. **Restore packages**

```bash
dotnet restore
```

3. **Cập nhật connection string**

   Chỉnh sửa [`appsettings.json`](src/EnterpriseAuth.API/appsettings.json:1) và [`appsettings.Development.json`](src/EnterpriseAuth.API/appsettings.Development.json:1):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseAuthDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

4. **Chạy migrations**

```bash
dotnet ef database update --project src/EnterpriseAuth.Infrastructure --startup-project src/EnterpriseAuth.API
```

5. **Chạy ứng dụng**

```bash
dotnet run --project src/EnterpriseAuth.API
```

6. **Truy cập Swagger UI**

   Mở trình duyệt và truy cập: `http://localhost:5066`

## 📚 API Endpoints

### Authentication

#### POST /api/auth/register

Đăng ký người dùng mới

**Request Body:**

```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!"
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "user": {
    "id": "guid",
    "username": "testuser",
    "email": "test@example.com"
  }
}
```

#### POST /api/auth/login

Đăng nhập người dùng

**Request Body:**

```json
{
  "username": "testuser",
  "password": "Password123!"
}
```

#### POST /api/auth/refresh

Làm mới access token

**Request Body:**

```json
{
  "refreshToken": "refresh_token_here"
}
```

#### POST /api/auth/logout

Đăng xuất người dùng

**Headers:**

```
Authorization: Bearer your_access_token_here
```

### Health Checks

#### GET /health

Kiểm tra trạng thái ứng dụng và database

## 🔧 Cấu hình

### JWT Settings

Cấu hình JWT trong [`appsettings.json`](src/EnterpriseAuth.API/appsettings.json:8):

```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "EnterpriseAuth.API",
    "Audience": "EnterpriseAuth.Client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true,
    "ClockSkewMinutes": 5
  }
}
```

### Logging Configuration

Serilog được cấu hình để ghi log ra console và file:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/enterprise-auth-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## 🏗️ Kiến trúc

### Clean Architecture Layers

1. **Domain Layer** ([`src/EnterpriseAuth.Domain`](src/EnterpriseAuth.Domain/))

   - Chứa business entities, domain interfaces, và business rules
   - Không phụ thuộc vào bất kỳ layer nào khác
   - Entities: [`User`](src/EnterpriseAuth.Domain/Entities/User.cs:1), [`Role`](src/EnterpriseAuth.Domain/Entities/Role.cs:1), [`Permission`](src/EnterpriseAuth.Domain/Entities/Permission.cs:1), [`RefreshToken`](src/EnterpriseAuth.Domain/Entities/RefreshToken.cs:1)

2. **Application Layer** ([`src/EnterpriseAuth.Application`](src/EnterpriseAuth.Application/))

   - Chứa business logic, application services, và DTOs
   - Phụ thuộc vào Domain Layer
   - Services: [`AuthService`](src/EnterpriseAuth.Application/Services/AuthService.cs:1), [`JwtService`](src/EnterpriseAuth.Application/Services/JwtService.cs:1)

3. **Infrastructure Layer** ([`src/EnterpriseAuth.Infrastructure`](src/EnterpriseAuth.Infrastructure/))

   - Implement các interfaces từ Domain và Application layers
   - Database access, external services
   - Repository pattern implementation

4. **Presentation Layer** ([`src/EnterpriseAuth.API`](src/EnterpriseAuth.API/))
   - Web API controllers, middleware, configuration
   - Entry point của ứng dụng

### Security Features

- **Password Hashing**: BCrypt với salt tự động
- **JWT Tokens**: Stateless authentication với refresh token mechanism
- **Role-based Access Control**: Flexible permission system
- **Request Validation**: FluentValidation cho tất cả input
- **Exception Handling**: Global exception middleware với structured error responses

## 🧪 Testing

### Chạy Unit Tests

```bash
dotnet test tests/EnterpriseAuth.UnitTests/
```

### Chạy Integration Tests

```bash
dotnet test tests/EnterpriseAuth.IntegrationTests/
```

### Chạy tất cả tests

```bash
dotnet test
```

## 📦 Deployment

### Docker Support

Tạo [`Dockerfile`](Dockerfile):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/EnterpriseAuth.API/EnterpriseAuth.API.csproj", "src/EnterpriseAuth.API/"]
COPY ["src/EnterpriseAuth.Application/EnterpriseAuth.Application.csproj", "src/EnterpriseAuth.Application/"]
COPY ["src/EnterpriseAuth.Domain/EnterpriseAuth.Domain.csproj", "src/EnterpriseAuth.Domain/"]
COPY ["src/EnterpriseAuth.Infrastructure/EnterpriseAuth.Infrastructure.csproj", "src/EnterpriseAuth.Infrastructure/"]
RUN dotnet restore "src/EnterpriseAuth.API/EnterpriseAuth.API.csproj"
COPY . .
WORKDIR "/src/src/EnterpriseAuth.API"
RUN dotnet build "EnterpriseAuth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnterpriseAuth.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseAuth.API.dll"]
```

### Build và chạy Docker container

```bash
docker build -t enterprise-auth .
docker run -p 8080:80 enterprise-auth
```

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add some amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Tạo Pull Request

## 📝 License

Dự án này được phân phối dưới MIT License. Xem [`LICENSE`](LICENSE) để biết thêm thông tin.

## 👥 Tác giả

- **Development Team** - _Initial work_ - [GitHub](https://github.com/yourusername)

## 📞 Hỗ trợ

Nếu bạn gặp vấn đề hoặc có câu hỏi, vui lòng tạo issue trên GitHub repository.

---

⭐ **Nếu dự án này hữu ích, hãy cho chúng tôi một star!** ⭐
