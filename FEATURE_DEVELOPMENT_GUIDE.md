# Hướng dẫn thêm chức năng mới - Enterprise Auth System

## Tổng quan

File README này mô tả quy trình chi tiết để thêm một chức năng mới vào hệ thống Enterprise Authentication, sử dụng tính năng "Quản lý sản phẩm" làm ví dụ minh họa. Hệ thống được xây dựng theo kiến trúc Clean Architecture với các layer: Domain, Application, Infrastructure và API.

## Kiến trúc hệ thống hiện tại

```
src/
├── EnterpriseAuth.Domain/           # Core domain logic
│   ├── Common/                      # Shared domain classes
│   ├── Entities/                    # Domain entities
│   ├── Interfaces/                  # Repository interfaces
│   └── Exceptions/                  # Domain exceptions
├── EnterpriseAuth.Application/      # Application services & DTOs
│   ├── DTOs/                        # Data transfer objects
│   ├── Interfaces/                  # Service interfaces
│   ├── Services/                    # Business logic services
│   └── Validators/                  # Input validation
├── EnterpriseAuth.Infrastructure/   # Data access & external services
│   ├── Data/                        # DbContext
│   ├── Configurations/              # EF configurations
│   ├── Repositories/                # Repository implementations
│   └── Migrations/                  # Database migrations
└── EnterpriseAuth.API/             # Web API layer
    ├── Controllers/                 # API controllers
    ├── Middleware/                  # Custom middleware
    └── Extensions/                  # Service extensions
```

## Ví dụ: Thêm chức năng "Quản lý sản phẩm"

### Bước 1: Thiết kế và phân tích yêu cầu

#### 1.1 Xác định yêu cầu chức năng

- Thêm sản phẩm mới
- Xem danh sách sản phẩm
- Cập nhật thông tin sản phẩm
- Xóa sản phẩm (soft delete)
- Tìm kiếm và phân trang

#### 1.2 Thiết kế entity Product

- Id (Guid)
- Name (string)
- Description (string)
- Price (decimal)
- SKU (string)
- Category (string)
- IsActive (bool)
- Các trường audit từ [`BaseEntity`](src/EnterpriseAuth.Domain/Common/BaseEntity.cs:1)

#### 1.3 Xác định permissions cần thiết

- `products.read` - Xem sản phẩm
- `products.write` - Tạo/cập nhật sản phẩm
- `products.delete` - Xóa sản phẩm

### Bước 2: Phát triển Domain Layer

#### 2.1 Tạo Entity Product

Tạo file [`src/EnterpriseAuth.Domain/Entities/Product.cs`](src/EnterpriseAuth.Domain/Entities/Product.cs:1):

```csharp
using EnterpriseAuth.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseAuth.Domain.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Domain methods
        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentException("Price cannot be negative");

            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

#### 2.2 Tạo Repository Interface

Tạo file [`src/EnterpriseAuth.Domain/Interfaces/IProductRepository.cs`](src/EnterpriseAuth.Domain/Interfaces/IProductRepository.cs:1):

```csharp
using EnterpriseAuth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseAuth.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetBySKUAsync(string sku);
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? category = null);
        Task<bool> SKUExistsAsync(string sku, Guid? excludeId = null);
    }
}
```

#### 2.3 Tạo Domain Exceptions

Tạo file [`src/EnterpriseAuth.Domain/Exceptions/ProductNotFoundException.cs`](src/EnterpriseAuth.Domain/Exceptions/ProductNotFoundException.cs:1):

```csharp
namespace EnterpriseAuth.Domain.Exceptions
{
    public class ProductNotFoundException : DomainException
    {
        public ProductNotFoundException(Guid productId)
            : base($"Product with ID {productId} was not found.")
        {
        }

        public ProductNotFoundException(string sku)
            : base($"Product with SKU {sku} was not found.")
        {
        }
    }
}
```

### Bước 3: Phát triển Application Layer

#### 3.1 Tạo DTOs

Tạo file [`src/EnterpriseAuth.Application/DTOs/ProductDto.cs`](src/EnterpriseAuth.Application/DTOs/ProductDto.cs:1):

```csharp
using System;

namespace EnterpriseAuth.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ProductPagedResponseDto
    {
        public IEnumerable<ProductDto> Items { get; set; } = new List<ProductDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
```

#### 3.2 Tạo Service Interface

Tạo file [`src/EnterpriseAuth.Application/Interfaces/IProductService.cs`](src/EnterpriseAuth.Application/Interfaces/IProductService.cs:1):

```csharp
using EnterpriseAuth.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseAuth.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto> GetByIdAsync(Guid id);
        Task<ProductDto> GetBySKUAsync(string sku);
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category);
        Task<ProductPagedResponseDto> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? category = null);
        Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> SKUExistsAsync(string sku, Guid? excludeId = null);
    }
}
```

#### 3.3 Tạo Validators

Tạo file [`src/EnterpriseAuth.Application/Validators/CreateProductValidator.cs`](src/EnterpriseAuth.Application/Validators/CreateProductValidator.cs:1):

```csharp
using FluentValidation;
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Application.Interfaces;

namespace EnterpriseAuth.Application.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        private readonly IProductService _productService;

        public CreateProductValidator(IProductService productService)
        {
            _productService = productService;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm là bắt buộc")
                .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU là bắt buộc")
                .MaximumLength(50).WithMessage("SKU không được vượt quá 50 ký tự")
                .MustAsync(BeUniqueSKU).WithMessage("SKU đã tồn tại");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Danh mục là bắt buộc")
                .MaximumLength(100).WithMessage("Danh mục không được vượt quá 100 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự");
        }

        private async Task<bool> BeUniqueSKU(string sku, CancellationToken cancellationToken)
        {
            return !await _productService.SKUExistsAsync(sku);
        }
    }
}
```

#### 3.4 Implement Service

Tạo file [`src/EnterpriseAuth.Application/Services/ProductService.cs`](src/EnterpriseAuth.Application/Services/ProductService.cs:1):

```csharp
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Application.Interfaces;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseAuth.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                SKU = dto.SKU,
                Category = dto.Category,
                IsActive = true
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(product);
        }

        public async Task<ProductDto> GetByIdAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new ProductNotFoundException(id);

            return MapToDto(product);
        }

        public async Task<ProductDto> GetBySKUAsync(string sku)
        {
            var product = await _unitOfWork.Products.GetBySKUAsync(sku);
            if (product == null)
                throw new ProductNotFoundException(sku);

            return MapToDto(product);
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return products.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category)
        {
            var products = await _unitOfWork.Products.GetByCategoryAsync(category);
            return products.Select(MapToDto);
        }

        public async Task<ProductPagedResponseDto> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? category = null)
        {
            var (items, totalCount) = await _unitOfWork.Products.GetPagedAsync(pageNumber, pageSize, searchTerm, category);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new ProductPagedResponseDto
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new ProductNotFoundException(id);

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.UpdatePrice(dto.Price);
            product.Category = dto.Category;
            product.IsActive = dto.IsActive;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(product);
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                throw new ProductNotFoundException(id);

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> SKUExistsAsync(string sku, Guid? excludeId = null)
        {
            return await _unitOfWork.Products.SKUExistsAsync(sku, excludeId);
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SKU = product.SKU,
                Category = product.Category,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}
```

### Bước 4: Phát triển Infrastructure Layer

#### 4.1 Tạo Entity Configuration

Tạo file [`src/EnterpriseAuth.Infrastructure/Configurations/ProductConfiguration.cs`](src/EnterpriseAuth.Infrastructure/Configurations/ProductConfiguration.cs:1):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseAuth.Domain.Entities;

namespace EnterpriseAuth.Infrastructure.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Create unique index for SKU
            builder.HasIndex(p => p.SKU)
                .IsUnique()
                .HasDatabaseName("IX_Products_SKU");

            // Create index for category
            builder.HasIndex(p => p.Category)
                .HasDatabaseName("IX_Products_Category");

            // Create index for IsActive
            builder.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Products_IsActive");

            // Configure audit fields from BaseEntity
            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.Property(p => p.CreatedBy)
                .HasMaxLength(100);

            builder.Property(p => p.UpdatedBy)
                .HasMaxLength(100);

            builder.Property(p => p.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Global query filter for soft delete
            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
```

#### 4.2 Implement Repository

Tạo file [`src/EnterpriseAuth.Infrastructure/Repositories/ProductRepository.cs`](src/EnterpriseAuth.Infrastructure/Repositories/ProductRepository.cs:1):

```csharp
using Microsoft.EntityFrameworkCore;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnterpriseAuth.Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Product?> GetBySKUAsync(string sku)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(p => p.Category == category)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await _dbSet
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? category = null)
        {
            var query = _dbSet.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.SKU.ToLower().Contains(searchTerm));
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> SKUExistsAsync(string sku, Guid? excludeId = null)
        {
            var query = _dbSet.Where(p => p.SKU == sku);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
```

#### 4.3 Cập nhật Unit of Work

Cập nhật interface [`src/EnterpriseAuth.Domain/Interfaces/IUnitOfWork.cs`](src/EnterpriseAuth.Domain/Interfaces/IUnitOfWork.cs:1):

```csharp
using System;
using System.Threading.Tasks;
using EnterpriseAuth.Domain.Entities;

namespace EnterpriseAuth.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<Permission> Permissions { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<RolePermission> RolePermissions { get; }
        IRefreshTokenRepository RefreshTokens { get; }

        // Thêm Products repository
        IProductRepository Products { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
```

Cập nhật implementation [`src/EnterpriseAuth.Infrastructure/Repositories/UnitOfWork.cs`](src/EnterpriseAuth.Infrastructure/Repositories/UnitOfWork.cs:1):

```csharp
using Microsoft.EntityFrameworkCore.Storage;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace EnterpriseAuth.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        private IUserRepository? _users;
        private IRepository<Role>? _roles;
        private IRepository<Permission>? _permissions;
        private IRepository<UserRole>? _userRoles;
        private IRepository<RolePermission>? _rolePermissions;
        private IRefreshTokenRepository? _refreshTokens;
        private IProductRepository? _products; // Thêm products

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
        public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
        public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
        public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);

        // Thêm Products property
        public IProductRepository Products => _products ??= new ProductRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
```

#### 4.4 Cập nhật ApplicationDbContext

Cập nhật file [`src/EnterpriseAuth.Infrastructure/Data/ApplicationDbContext.cs`](src/EnterpriseAuth.Infrastructure/Data/ApplicationDbContext.cs:1):

```csharp
// Thêm vào phần DbSet
public DbSet<Product> Products { get; set; }

// Thêm vào OnModelCreating method
modelBuilder.ApplyConfiguration(new ProductConfiguration());

// Thêm seed data cho permissions (trong SeedData method)
new Permission
{
    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    Name = "products.read",
    Description = "Read products",
    Resource = "products",
    Action = "read",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
},
new Permission
{
    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
    Name = "products.write",
    Description = "Write products",
    Resource = "products",
    Action = "write",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
},
new Permission
{
    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    Name = "products.delete",
    Description = "Delete products",
    Resource = "products",
    Action = "delete",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
}
```

#### 4.4 Tạo Migration

Chạy lệnh để tạo migration:

```bash
cd src/EnterpriseAuth.Infrastructure
dotnet ef migrations add AddProductEntity --project . --startup-project ../EnterpriseAuth.API
dotnet ef database update --project . --startup-project ../EnterpriseAuth.API
```

### Bước 5: Phát triển API Layer

#### 5.1 Tạo Products Controller

Tạo file [`src/EnterpriseAuth.API/Controllers/ProductsController.cs`](src/EnterpriseAuth.API/Controllers/ProductsController.cs:1):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Application.Interfaces;
using EnterpriseAuth.Domain.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace EnterpriseAuth.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "products.read")]
        [ProducesResponseType(typeof(ProductPagedResponseDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var result = await _productService.GetPagedAsync(pageNumber, pageSize, searchTerm, category);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error retrieving products" });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "products.read")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);
                return Ok(product);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error retrieving product" });
            }
        }

        /// <summary>
        /// Create new product
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "products.write")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                var product = await _productService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error creating product" });
            }
        }

        /// <summary>
        /// Update existing product
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "products.write")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                var product = await _productService.UpdateAsync(id, dto);
                return Ok(product);
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error updating product" });
            }
        }

        /// <summary>
        /// Delete product
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "products.delete")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                return NoContent();
            }
            catch (ProductNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error deleting product" });
            }
        }

        /// <summary>
        /// Get products by category
        /// </summary>
        [HttpGet("category/{category}")]
        [Authorize(Policy = "products.read")]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProductsByCategory(string category)
        {
            try
            {
                var products = await _productService.GetByCategoryAsync(category);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category {Category}", category);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new ProblemDetails { Title = "Error retrieving products" });
            }
        }
    }
}
```

#### 5.2 Cập nhật Service Registration

Cập nhật file [`src/EnterpriseAuth.API/Extensions/ServiceExtensions.cs`](src/EnterpriseAuth.API/Extensions/ServiceExtensions.cs:1):

```csharp
// Thêm vào DI container (trong AddApplicationServices method)
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IValidator<CreateProductDto>, CreateProductValidator>();
services.AddScoped<IValidator<UpdateProductDto>, UpdateProductValidator>();

// Lưu ý: IProductRepository đã được register thông qua UnitOfWork

// Thêm authorization policies
services.AddAuthorization(options =>
{
    options.AddPolicy("products.read", policy =>
        policy.RequireClaim("permission", "products.read"));
    options.AddPolicy("products.write", policy =>
        policy.RequireClaim("permission", "products.write"));
    options.AddPolicy("products.delete", policy =>
        policy.RequireClaim("permission", "products.delete"));
});
```

### Bước 6: Testing

#### 6.1 Unit Tests

Tạo file [`tests/EnterpriseAuth.UnitTests/Services/ProductServiceTests.cs`](tests/EnterpriseAuth.UnitTests/Services/ProductServiceTests.cs:1):

```csharp
using Xunit;
using Moq;
using EnterpriseAuth.Application.Services;
using EnterpriseAuth.Application.DTOs;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Exceptions;
using System;
using System.Threading.Tasks;

namespace EnterpriseAuth.UnitTests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockRepository;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockRepository = new Mock<IProductRepository>();
            _productService = new ProductService(_mockRepository.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsProductDto()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100m,
                SKU = "TEST001",
                Category = "Electronics"
            };

            _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _productService.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.SKU, result.SKU);
            _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ThrowsProductNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockProductRepository.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(
                () => _productService.GetByIdAsync(id));
        }
    }
}
```

#### 6.2 Integration Tests

Tạo file [`tests/EnterpriseAuth.IntegrationTests/Controllers/ProductsControllerTests.cs`](tests/EnterpriseAuth.IntegrationTests/Controllers/ProductsControllerTests.cs:1):

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using EnterpriseAuth.Application.DTOs;
using System.Net;
using System.Threading.Tasks;

namespace EnterpriseAuth.IntegrationTests.Controllers
{
    public class ProductsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ProductsControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetProducts_WithoutAuth_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/products");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_ValidData_ReturnsCreated()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100m,
                SKU = "TEST001",
                Category = "Electronics"
            };

            var json = JsonSerializer.Serialize(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authentication header (implement based on your auth setup)
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-token");

            // Act
            var response = await _client.PostAsync("/api/products", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
```

### Bước 7: Documentation

#### 7.1 API Documentation

Cập nhật Swagger documentation trong [`src/EnterpriseAuth.API/Program.cs`](src/EnterpriseAuth.API/Program.cs:1):

```csharp
// Thêm XML documentation cho Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "EnterpriseAuth.API.xml"));
    // Include other XML files for complete documentation
});
```

#### 7.2 User Guide

Tạo file [`docs/ProductManagement.md`](docs/ProductManagement.md:1) với hướng dẫn sử dụng API.

### Bước 8: Deployment

#### 8.1 Environment Configuration

Cập nhật [`src/EnterpriseAuth.API/appsettings.json`](src/EnterpriseAuth.API/appsettings.json:1):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EnterpriseAuthDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "ProductSettings": {
    "DefaultPageSize": 10,
    "MaxPageSize": 100
  }
}
```

#### 8.2 Docker Support

Cập nhật [`docker-compose.yml`](docker-compose.yml:1) nếu cần thiết.

## Checklist hoàn thành tính năng

### Domain Layer

- [ ] Tạo Entity Product kế thừa từ BaseEntity
- [ ] Định nghĩa IProductRepository interface
- [ ] Tạo Domain exceptions (ProductNotFoundException)
- [ ] Implement domain methods trong Product entity

### Application Layer

- [ ] Tạo DTOs (ProductDto, CreateProductDto, UpdateProductDto, ProductPagedResponseDto)
- [ ] Định nghĩa IProductService interface
- [ ] Implement ProductService với business logic
- [ ] Tạo FluentValidation validators
- [ ] Configure AutoMapper (nếu sử dụng)

### Infrastructure Layer

- [ ] Tạo ProductConfiguration cho Entity Framework
- [ ] Implement ProductRepository với database operations
- [ ] Cập nhật ApplicationDbContext
- [ ] Tạo và chạy database migrations
- [ ] Seed default permissions cho products

### API Layer

- [ ] Tạo ProductsController với CRUD endpoints
- [ ] Configure authorization policies
- [ ] Thêm proper error handling và logging
- [ ] Update service registration trong DI container
- [ ] Configure Swagger documentation

### Testing

- [ ] Viết unit tests cho ProductService
- [ ] Tạo integration tests cho ProductsController
- [ ] Test validation scenarios
- [ ] Test authorization và permissions
- [ ] Performance testing cho large datasets

### Documentation

- [ ] Cập nhật API documentation (Swagger)
- [ ] Viết user guide cho tính năng mới
- [ ] Update code comments và inline documentation
- [ ] Cập nhật README và changelog

### Deployment

- [ ] Test trên staging environment
- [ ] Configure monitoring và health checks
- [ ] Update database connection strings
- [ ] Deploy lên production environment
- [ ] Verify functionality post-deployment

## Best Practices được áp dụng

1. **Clean Architecture**: Tách biệt rõ ràng các layer, dependency injection
2. **SOLID Principles**: Single responsibility, dependency inversion
3. **Domain-Driven Design**: Rich domain models, repository pattern
4. **Security**: Authorization policies, input validation
5. **Performance**: Pagination, indexing, async/await
6. **Testing**: Unit tests, integration tests, mocking
7. **Logging**: Structured logging với Serilog
8. **Error Handling**: Global exception handling, proper HTTP status codes
9. **Documentation**: XML documentation, Swagger, user guides

## Lưu ý quan trọng

- Luôn backup database trước khi chạy migrations
- Test thoroughly trên staging trước khi deploy production
- Monitor performance sau khi deploy tính năng mới
- Ensure proper error handling và logging
- Follow existing code conventions và naming standards
- Update permissions và roles theo business requirements
