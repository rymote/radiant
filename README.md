# Rymote.Radiant

A modern, lightweight ORM for .NET with source generators for type-safe database operations. Rymote.Radiant provides a fluent API for building queries and uses source generators to create strongly-typed extension methods based on your model properties.

## Features

- 🚀 **Source Generators** - Automatically generates type-safe query methods based on your models
- 🔧 **Fluent Query Builder** - Intuitive API for building complex queries
- 📦 **Bulk Operations** - Efficient bulk insert, update, and delete operations
- 🎯 **Type Safety** - Compile-time checking for column names and query methods
- 🗄️ **PostgreSQL Support** - Built with PostgreSQL in mind (easily extensible to other databases)
- 🔄 **Migration Support** - Works seamlessly with FluentMigrator
- ⚡ **High Performance** - Built on top of Dapper for optimal performance

## Installation

```bash
dotnet add package Rymote.Radiant
dotnet add package Rymote.Radiant.Generator
```

## Quick Start

### 1. Define Your Model

```csharp
using Rymote.Radiant.Core.Attributes;
using Rymote.Radiant.Models;

[Table("users")]
public class User : Model<long>
{
    [Column("first_name")]
    public string FirstName { get; set; } = "";

    [Column("last_name")]
    public string LastName { get; set; } = "";
    
    [Column("email")]
    public string Email { get; set; } = "";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
```

### 2. Configure the Connection

```csharp
using var connection = new NpgsqlConnection(connectionString);
Model<long>.Configure(connection);
```

### 3. Use the Generated Methods

The source generator automatically creates extension methods for your models:

```csharp
// Query users by first name
var johns = await User.Query<User>()
    .WhereFirstName("John")
    .OrderByCreatedAtDesc()
    .GetAsync();

// Find users with email containing a domain
var gmailUsers = await User.Query<User>()
    .WhereEmailContains("@gmail.com")
    .GetAsync();

// Complex queries
var recentUsers = await User.Query<User>()
    .WhereCreatedAtGreaterThan(DateTime.UtcNow.AddDays(-30))
    .WhereEmailIsNotNull()
    .OrderByLastName()
    .Limit(10)
    .GetAsync();
```

## Generated Methods

For each property in your model, Rymote.Radiant generates:

### Query Methods
- `Where{Property}(value)` - Exact match
- `Where{Property}In(values)` - IN clause
- `Where{Property}NotIn(values)` - NOT IN clause
- `Where{Property}IsNull()` - NULL check
- `Where{Property}IsNotNull()` - NOT NULL check

### String-specific Methods
- `Where{Property}Like(pattern)` - LIKE pattern matching
- `Where{Property}StartsWith(prefix)` - Starts with
- `Where{Property}EndsWith(suffix)` - Ends with
- `Where{Property}Contains(substring)` - Contains

### Numeric/Date Methods
- `Where{Property}GreaterThan(value)`
- `Where{Property}LessThan(value)`
- `Where{Property}Between(start, end)`

### DateTime-specific Methods
- `Where{Property}Date(date)` - Match by date
- `Where{Property}Year(year)` - Match by year
- `Where{Property}Month(month)` - Match by month

### Ordering Methods
- `OrderBy{Property}()` - Ascending order
- `OrderBy{Property}Desc()` - Descending order

## CRUD Operations

### Create
```csharp
var user = new User
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john@example.com",
    CreatedAt = DateTimeOffset.UtcNow
};

// Using SaveAsync
await user.SaveAsync<User>();

// Or using static method
var created = await User.CreateAsync(user);
```

### Read
```csharp
// Find by ID
var user = await User.FindAsync<User>(123);

// Find with generated extension method
var user = await connection.FindAsync(123L);

// Query with conditions
var users = await User.Query<User>()
    .WhereLastName("Doe")
    .GetAsync();

// Get all
var allUsers = await User.AllAsync<User>();
```

### Update
```csharp
// Update using instance method
user.Email = "newemail@example.com";
await user.SaveAsync<User>();

// Update using generated extension
await connection.UpdateAsync(user);

// Update using static method
await User.UpdateAsync<User>(123, new { Email = "updated@example.com" });
```

### Delete
```csharp
// Delete using instance method
await user.DeleteAsync();

// Delete using generated extension
await connection.DeleteAsync(123L);

// Delete using static method
await User.DeleteAsync<User>(123);
```

## Bulk Operations

```csharp
// Bulk insert
var users = new List<User> { /* ... */ };
await connection.BulkInsertAsync(users);

// Bulk update
await connection.BulkUpdateAsync(users);

// Bulk delete
var ids = new long[] { 1, 2, 3, 4, 5 };
await connection.BulkDeleteAsync(ids);
```

## Advanced Queries

### Pagination
```csharp
var paginatedResult = await User.QueryPaginated<User>()
    .WhereEmailIsNotNull()
    .OrderByCreatedAtDesc()
    .Paginate(page: 1, pageSize: 20)
    .GetAsync();

Console.WriteLine($"Total: {paginatedResult.Total}");
Console.WriteLine($"Pages: {paginatedResult.TotalPages}");
```

### Complex Conditions
```csharp
var users = await User.Query<User>()
    .WhereFirstNameStartsWith("J")
    .Where(UserExtensions.Columns.CreatedAt, ">", DateTime.UtcNow.AddDays(-7))
    .WhereIn(UserExtensions.Columns.LastName, new[] { "Smith", "Jones" })
    .OrderBy(UserExtensions.Columns.LastName)
    .GetAsync();
```

### Raw SQL with Column Constants
```csharp
// Use generated column constants for type-safe raw SQL
var sql = $@"
    SELECT {UserExtensions.Columns.FirstName}, 
           {UserExtensions.Columns.LastName}, 
           COUNT(*) as count
    FROM users 
    WHERE {UserExtensions.Columns.CreatedAt} > @date
    GROUP BY {UserExtensions.Columns.FirstName}, 
             {UserExtensions.Columns.LastName}";

var results = await connection.QueryAsync(sql, new { date = DateTime.UtcNow.AddDays(-30) });
```

## Attributes

### Table Attribute
```csharp
[Table("users", Schema = "public")]
public class User : Model<long> { }
```

### Column Attribute
```csharp
[Column("first_name")]
public string FirstName { get; set; }
```

### PrimaryKey Attribute
```csharp
[PrimaryKey(AutoGenerated = true)]
public long Id { get; set; }
```

### JsonColumn Attribute
```csharp
[JsonColumn]
public Dictionary<string, object> Metadata { get; set; }
```

## Query Builder Methods

The QueryBuilder provides a fluent API for building queries:

- `Select(columns)` - Specify columns to select
- `Where(column, operator, value)` - Add WHERE condition
- `WhereIn(column, values)` - IN clause
- `WhereBetween(column, start, end)` - BETWEEN clause
- `WhereNull(column)` / `WhereNotNull(column)` - NULL checks
- `WhereLike(column, pattern)` - LIKE pattern matching
- `WhereJsonContains(column, value)` - JSON containment (PostgreSQL)
- `Join(table, condition)` - JOIN operations
- `GroupBy(columns)` - GROUP BY clause
- `Having(condition)` - HAVING clause
- `OrderBy(column)` / `OrderByDesc(column)` - Ordering
- `Limit(count)` / `Offset(count)` - Pagination
- `GetAsync()` - Execute and return results
- `FirstAsync()` / `FirstOrDefaultAsync()` - Get single result
- `CountAsync()` - Get count
- `ExistsAsync()` - Check existence

## Integration with FluentMigrator

```csharp
[Migration(202401010001)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}
```

## Configuration

### Global Configuration
```csharp
// Set default schema
Model<long>.Configure(connection, schema: "myschema");
```

### Connection Management
The library uses a static connection configuration. For multiple databases or advanced scenarios, consider using dependency injection or connection factories.

## Requirements

- .NET 6.0 or later
- C# 9.0 or later (for source generators)
- PostgreSQL (primary support, other databases can be added)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Production Usage & Scalability

### Using in Large-Scale Applications

Rymote.Radiant is built on top of Dapper, which is battle-tested in high-traffic applications. However, the current implementation uses a static connection configuration, which requires some adjustments for production use:

#### Connection Pooling

PostgreSQL (via Npgsql) handles connection pooling automatically. Configure it in your connection string:

```csharp
var connectionString = "Host=localhost;Database=mydb;Username=user;Password=pass;" +
                      "Pooling=true;Minimum Pool Size=10;Maximum Pool Size=100;" +
                      "Connection Lifetime=300;Connection Idle Lifetime=60";
```

### ASP.NET Core Integration

For ASP.NET Core applications, you'll want to use dependency injection and proper connection management:

#### Simplified DI Configuration

##### Option 1: Scoped Database Context

Create a database context that handles configuration automatically:

```csharp
public interface IRadiantContext : IDisposable
{
    IDbConnection Connection { get; }
    QueryBuilder<T> Query<T>() where T : class;
    Task<T?> FindAsync<T>(long id) where T : class;
    Task<T> CreateAsync<T>(T model) where T : class;
    Task<int> UpdateAsync<T>(long id, object values) where T : class;
    Task<int> DeleteAsync<T>(long id) where T : class;
}

public class RadiantContext : IRadiantContext
{
    public IDbConnection Connection { get; }

    public RadiantContext(IDbConnectionFactory connectionFactory)
    {
        Connection = connectionFactory.CreateConnection();
        // Configure once when context is created
        Model<long>.Configure(Connection);
    }

    public QueryBuilder<T> Query<T>() where T : class
    {
        return Model<long>.Query<T>();
    }

    public async Task<T?> FindAsync<T>(long id) where T : class
    {
        return await Model<long>.FindAsync<T>(id);
    }

    public async Task<T> CreateAsync<T>(T model) where T : class
    {
        return await Model<long>.CreateAsync(model);
    }

    public async Task<int> UpdateAsync<T>(long id, object values) where T : class
    {
        return await Model<long>.UpdateAsync<T>(id, values);
    }

    public async Task<int> DeleteAsync<T>(long id) where T : class
    {
        return await Model<long>.DeleteAsync<T>(id);
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}

// Register in Program.cs
builder.Services.AddScoped<IRadiantContext, RadiantContext>();

// Use in controllers/services
public class UserService
{
    private readonly IRadiantContext _context;

    public UserService(IRadiantContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserAsync(long id)
    {
        return await _context.FindAsync<User>(id);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _context.Query<User>()
            .WhereEmailIsNotNull()
            .OrderByCreatedAtDesc()
            .GetAsync();
    }
}
```

##### Option 2: Extension Method for Service Collection

Create an extension method to simplify registration:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRadiant(this IServiceCollection services, 
        string connectionString, 
        string schema = "public")
    {
        // Register connection factory
        services.AddSingleton<IDbConnectionFactory>(sp => 
            new NpgsqlConnectionFactory(connectionString));
        
        // Register context
        services.AddScoped<IRadiantContext, RadiantContext>();
        
        // Register a factory for creating configured connections
        services.AddScoped<Func<IDbConnection>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbConnectionFactory>();
            return () =>
            {
                var connection = factory.CreateConnection();
                Model<long>.Configure(connection, schema);
                return connection;
            };
        });
        
        return services;
    }
}

// In Program.cs - One line configuration!
builder.Services.AddRadiant(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    schema: "public"
);
```

##### Option 3: Generic Repository with Auto-Configuration

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(long id);
    QueryBuilder<T> Query();
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly Lazy<IDbConnection> _connection;

    public Repository(IDbConnectionFactory connectionFactory)
    {
        _connection = new Lazy<IDbConnection>(() =>
        {
            var conn = connectionFactory.CreateConnection();
            Model<long>.Configure(conn);
            return conn;
        });
    }

    protected IDbConnection Connection => _connection.Value;

    public async Task<T?> GetByIdAsync(long id)
    {
        return await Model<long>.FindAsync<T>(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Model<long>.AllAsync<T>();
    }

    public async Task<T> CreateAsync(T entity)
    {
        return await Model<long>.CreateAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        // Use the generated extension method
        var method = typeof(UserExtensions).GetMethod("UpdateAsync");
        if (method != null)
        {
            await (Task<int>)method.Invoke(null, new object[] { Connection, entity });
        }
    }

    public async Task DeleteAsync(long id)
    {
        await Model<long>.DeleteAsync<T>(id);
    }

    public QueryBuilder<T> Query()
    {
        return Model<long>.Query<T>();
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
            _connection.Value?.Dispose();
    }
}

// Register generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Use in controllers
public class UsersController : ControllerBase
{
    private readonly IRepository<User> _userRepository;

    public UsersController(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.Query()
            .WhereEmailIsNotNull()
            .OrderByCreatedAtDesc()
            .GetAsync();
        
        return Ok(users);
    }
}
```

##### Option 4: Minimal API with Inline Configuration

For simpler applications or Minimal APIs:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Radiant with a simple helper
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connection = new NpgsqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection"));
    Model<long>.Configure(connection);
    return connection;
});

var app = builder.Build();

// Use directly in endpoints
app.MapGet("/users/{id}", async (long id, IDbConnection db) =>
{
    var user = await db.FindAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", async (User user, IDbConnection db) =>
{
    var created = await User.CreateAsync(user);
    return Results.Created($"/users/{created.Id}", created);
});
```

##### Option 5: Using IHostedService for One-Time Configuration

For scenarios where you want to configure once at startup:

```csharp
public class RadiantConfigurationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public RadiantConfigurationService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a connection just for configuration
        using var scope = _serviceProvider.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();
        
        Model<long>.Configure(connection, _configuration["Radiant:DefaultSchema"] ?? "public");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Register the hosted service
builder.Services.AddHostedService<RadiantConfigurationService>();
```

#### Complete Minimal Setup Example

Here's the absolute simplest way to use Radiant with DI in ASP.NET Core:

```csharp
var builder = WebApplication.CreateBuilder(args);

// One-line Radiant setup
builder.Services.AddRadiant(builder.Configuration.GetConnectionString("DefaultConnection"));

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Your app is ready to use Radiant!
app.MapControllers();
app.Run();
```

With the controller:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRadiantContext _context;

    public UsersController(IRadiantContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var user = await _context.FindAsync<User>(id);
        return user != null ? Ok(user) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Query<User>()
            .WhereEmailIsNotNull()
            .GetAsync();
        return Ok(users);
    }
}
```

These patterns make it much easier to use Rymote.Radiant with dependency injection while maintaining proper connection management and avoiding the complexity of manual configuration in every service.

## Strongly Typed IDs and ULID Support

### Using Strongly Typed IDs

Strongly typed IDs help prevent primitive obsession and make your code more type-safe:

```csharp
// Define a strongly typed ID
public readonly record struct UserId(long Value)
{
    public static implicit operator long(UserId id) => id.Value;
    public static implicit operator UserId(long value) => new(value);
}

// Model with strongly typed ID
[Table("users")]
public class User : Model<UserId>
{
    [Column("first_name")]
    public string FirstName { get; set; } = "";
    
    [Column("email")]
    public string Email { get; set; } = "";
}

// Usage
var user = await User.FindAsync<User>(new UserId(123));
// Or with implicit conversion
var user2 = await User.FindAsync<User>(123);

// In queries
var users = await User.Query<User>()
    .Where("id", "=", new UserId(456))
    .GetAsync();
```

### Using ULID as Primary Key

ULID provides sortable, globally unique identifiers that are more efficient than GUIDs:

```csharp
// First, install the ULID package
// dotnet add package Ulid

using System.ComponentModel;
using System.Globalization;

// ULID Type Converter for Dapper
public class UlidTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue)
            return Ulid.Parse(stringValue);
        
        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Ulid ulid)
            return ulid.ToString();
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

// Dapper Type Handler for ULID
public class UlidTypeHandler : SqlMapper.TypeHandler<Ulid>
{
    public override Ulid Parse(object value)
    {
        if (value is string stringValue)
            return Ulid.Parse(stringValue);
        
        throw new ArgumentException($"Unable to convert {value} to Ulid");
    }

    public override void SetValue(IDbDataParameter parameter, Ulid value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}

// Register the type handler at startup
SqlMapper.AddTypeHandler(new UlidTypeHandler());

// Model using ULID
[Table("products")]
public class Product : Model<Ulid>
{
    [Column("name")]
    public string Name { get; set; } = "";
    
    [Column("price")]
    public decimal Price { get; set; }
    
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// Migration for ULID table
[Migration(202401020001)]
public class CreateProductsTable : Migration
{
    public override void Up()
    {
        Create.Table("products")
            .WithColumn("id").AsString(26).PrimaryKey() // ULID is 26 characters
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("price").AsDecimal(10, 2).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("products");
    }
}

// Usage with ULID
var product = new Product
{
    Id = Ulid.NewUlid(),
    Name = "Laptop",
    Price = 999.99m,
    CreatedAt = DateTimeOffset.UtcNow
};

await product.SaveAsync<Product>();

// Query by ULID
var found = await Product.FindAsync<Product>(product.Id);

// ULID provides time-based sorting
var recentProducts = await Product.Query<Product>()
    .OrderByIdDesc() // ULIDs are sortable by creation time
    .Limit(10)
    .GetAsync();
```

### Combining Strongly Typed IDs with ULID

For maximum type safety and uniqueness:

```csharp
// Strongly typed ULID wrapper
public readonly record struct ProductId(Ulid Value) : IComparable<ProductId>
{
    public static ProductId New() => new(Ulid.NewUlid());
    
    public static implicit operator Ulid(ProductId id) => id.Value;
    public static implicit operator ProductId(Ulid value) => new(value);
    
    public int CompareTo(ProductId other) => Value.CompareTo(other.Value);
    
    public override string ToString() => Value.ToString();
    
    public static ProductId Parse(string value) => new(Ulid.Parse(value));
}

// Type handler for strongly typed ULID
public class ProductIdTypeHandler : SqlMapper.TypeHandler<ProductId>
{
    public override ProductId Parse(object value)
    {
        if (value is string stringValue)
            return ProductId.Parse(stringValue);
        
        throw new ArgumentException($"Unable to convert {value} to ProductId");
    }

    public override void SetValue(IDbDataParameter parameter, ProductId value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
    }
}

// Register at startup
SqlMapper.AddTypeHandler(new ProductIdTypeHandler());

// Model with strongly typed ULID
[Table("products")]
public class Product : Model<ProductId>
{
    [Column("name")]
    public string Name { get; set; } = "";
    
    [Column("sku")]
    public string Sku { get; set; } = "";
}

// Usage
var product = new Product
{
    Id = ProductId.New(),
    Name = "Gaming Laptop",
    Sku = "LAP-001"
};

await product.SaveAsync<Product>();

// Type-safe queries
ProductId searchId = ProductId.Parse("01ARZ3NDEKTSV4RRFFQ69G5FAV");
var found = await Product.FindAsync<Product>(searchId);
```

### Custom ID Generation Strategies

You can also implement custom ID generation:

```csharp
// Snowflake ID generator
public readonly record struct SnowflakeId(long Value)
{
    private static readonly SnowflakeIdGenerator Generator = new(1); // Machine ID = 1
    
    public static SnowflakeId New() => new(Generator.NextId());
    
    public static implicit operator long(SnowflakeId id) => id.Value;
    public static implicit operator SnowflakeId(long value) => new(value);
}

// NanoID for shorter, URL-safe IDs
public readonly record struct NanoId(string Value)
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Size = 21;
    
    public static NanoId New()
    {
        var id = Nanoid.Generate(Alphabet, Size);
        return new(id);
    }
    
    public static implicit operator string(NanoId id) => id.Value;
    public static implicit operator NanoId(string value) => new(value);
}

// Sequential GUID for SQL Server
public readonly record struct SequentialGuid(Guid Value)
{
    public static SequentialGuid New()
    {
        // Implementation that creates sequential GUIDs
        // Good for SQL Server clustered indexes
        return new(CreateSequentialGuid());
    }
    
    private static Guid CreateSequentialGuid()
    {
        // Implementation details...
    }
}
```

### Best Practices for IDs in Rymote.Radiant

1. **Choose the Right ID Type**:
   - `long` - Traditional auto-increment, good for internal systems
   - `ULID` - Sortable, globally unique, good for distributed systems
   - `Guid` - Globally unique but not sortable
   - Custom strongly typed - Best type safety

2. **Configure Your Generator**:
   ```csharp
   // For ULID models, override CreateAsync to set ID
   public class UlidModel<T> : Model<Ulid> where T : UlidModel<T>
   {
       public override async Task<T> SaveAsync<T>()
       {
           if (Id == default)
               Id = Ulid.NewUlid();
           
           return await base.SaveAsync<T>();
       }
   }
   ```

3. **Database Considerations**:
   - PostgreSQL: Use `varchar(26)` for ULID, `bigint` for long
   - SQL Server: Consider sequential GUIDs for clustered indexes
   - MySQL: Be careful with UUID performance

4. **Migration Example for Different ID Types**:
   ```csharp
   // ULID
   .WithColumn("id").AsString(26).PrimaryKey()
   
   // UUID/GUID
   .WithColumn("id").AsGuid().PrimaryKey()
   
   // Traditional auto-increment
   .WithColumn("id").AsInt64().PrimaryKey().Identity()
   ```

These examples show how Rymote.Radiant can work with various ID strategies, providing flexibility for different application requirements while maintaining type safety and performance.

#### 1. Create a Connection Factory

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
```

#### 2. Create a Scoped Service Wrapper

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(long id);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
        Model<long>.Configure(_connection);
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _connection.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await User.Query<User>()
            .WhereEmailIsNotNull()
            .OrderByCreatedAtDesc()
            .GetAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        return await User.CreateAsync(user);
    }

    public async Task UpdateAsync(User user)
    {
        await _connection.UpdateAsync(user);
    }

    public async Task DeleteAsync(long id)
    {
        await _connection.DeleteAsync(id);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

#### 3. Configure Services in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register connection factory
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// For FluentMigrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ScanIn(typeof(Program).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
```

#### 4. Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        var created = await _userRepository.CreateAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
    }
}
```

### Multi-Tenant Applications

For multi-tenant scenarios, you can extend the connection factory:

```csharp
public class TenantConnectionFactory : IDbConnectionFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public TenantConnectionFactory(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
        var connectionString = GetTenantConnectionString(tenantId);
        return new NpgsqlConnection(connectionString);
    }

    private string GetTenantConnectionString(string tenantId)
    {
        // Logic to get tenant-specific connection string
        // Could be from configuration, database, or computed
        return _configuration.GetConnectionString($"Tenant_{tenantId}");
    }
}
```

### Performance Considerations

1. **Connection Lifetime**: Don't keep connections open longer than necessary
2. **Async All The Way**: Use async methods throughout your call stack
3. **Batch Operations**: Use bulk operations for multiple records
4. **Indexing**: Ensure proper database indexes (the generated column constants help with this)
5. **Query Optimization**: Use `Select()` to limit columns returned

```csharp
// Good - only select needed columns
var names = await User.Query<User>()
    .Select(UserExtensions.Columns.FirstName, UserExtensions.Columns.LastName)
    .WhereCreatedAtGreaterThan(DateTime.UtcNow.AddDays(-7))
    .GetAsync();

// Use bulk operations for multiple records
var users = GenerateUsers(1000);
await connection.BulkInsertAsync(users); // Much faster than individual inserts
```

### Unit Testing

Create an interface for your models to enable mocking:

```csharp
public interface IUserModel
{
    Task<User> SaveAsync<T>() where T : class;
    Task<int> DeleteAsync();
}

// In tests
var mockUser = new Mock<IUserModel>();
mockUser.Setup(u => u.SaveAsync<User>()).ReturnsAsync(testUser);
```

### Limitations & Workarounds

1. **Static Configuration**: The current `Model<T>.Configure()` is static. In production, wrap it in scoped services.
2. **Single Database**: For multiple databases, create separate repository classes for each context.
3. **Transactions**: Use Dapper's transaction support:

```csharp
using var connection = _connectionFactory.CreateConnection();
connection.Open();
using var transaction = connection.BeginTransaction();

try
{
    // Configure with transaction
    Model<long>.Configure(connection);
    
    await User.CreateAsync(user1);
    await User.CreateAsync(user2);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Recommended Architecture for Large Applications

```
YourApp.Domain/
  - Models/
    - User.cs (with Rymote.Radiant attributes)
  
YourApp.Infrastructure/
  - Repositories/
    - UserRepository.cs
  - Data/
    - ConnectionFactory.cs
    
YourApp.Api/
  - Controllers/
    - UsersController.cs
  - Program.cs (DI configuration)
```

This separation allows you to:
- Keep your domain models clean
- Centralize data access logic
- Easily mock repositories for testing
- Switch databases if needed 