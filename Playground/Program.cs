using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Npgsql;
using Dapper;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql;
using Rymote.Radiant.Sql.Expressions;
using Playground.Models;

namespace Playground;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres";
        
        // Setup database
        await SetupDatabaseAsync(connectionString);
        
        // Configure SmartModel
        using IDbConnection connection = new NpgsqlConnection(connectionString);
        SmartModelConfiguration configuration = new SmartModelConfiguration();
        
        configuration
            .UseConnection(connection)
            .RegisterModel<User>()
            .RegisterModel<Address>()
            .RegisterModel<Category>()
            .RegisterModel<Product>()
            .RegisterModel<Order>()
            .RegisterModel<OrderItem>()
            .Build();
            
        SmartModel.Configure(connection, configuration.GetModelMetadataCache());
        
        // Run tests
        await RunTestsAsync(connection);
        
        Console.WriteLine("\nAll tests completed successfully!");
    }
    
    static async Task SetupDatabaseAsync(string connectionString)
    {
        Console.WriteLine("Setting up database...");
        
        using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Check if pgvector extension exists (optional)
        bool hasVectorSupport = false;
        try
        {
            await connection.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector");
            Console.WriteLine("pgvector extension enabled");
            hasVectorSupport = true;
        }
        catch
        {
            Console.WriteLine("pgvector extension not available - skipping vector features");
        }
        
        // Build setup script dynamically based on available features
        string embeddingColumn = hasVectorSupport ? "embedding VECTOR(384)," : "";
        
        // Execute setup script
        string setupScript = $@"
-- Drop existing tables if they exist
DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS product_categories CASCADE;
DROP TABLE IF EXISTS products CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS addresses CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- Create users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    profile_data JSONB DEFAULT '{{}}',
    tags TEXT[] DEFAULT '{{}}',
    {embeddingColumn}
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Create addresses table
CREATE TABLE addresses (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    street VARCHAR(255) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state VARCHAR(100),
    country VARCHAR(100) NOT NULL,
    postal_code VARCHAR(20),
    is_primary BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Create categories table
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    parent_id INTEGER REFERENCES categories(id) ON DELETE CASCADE,
    metadata JSONB DEFAULT '{{}}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Create products table
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    sku VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(10, 2) NOT NULL,
    stock_quantity INTEGER DEFAULT 0,
    attributes JSONB DEFAULT '{{}}',
    tags TEXT[] DEFAULT '{{}}',
    search_vector tsvector,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Create orders table
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    order_number VARCHAR(50) UNIQUE NOT NULL,
    user_id INTEGER NOT NULL REFERENCES users(id),
    shipping_address_id INTEGER REFERENCES addresses(id),
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    total_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    metadata JSONB DEFAULT '{{}}',
    placed_at TIMESTAMP,
    shipped_at TIMESTAMP,
    delivered_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Create order_items table
CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id INTEGER NOT NULL REFERENCES products(id),
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price DECIMAL(10, 2) NOT NULL,
    discount_amount DECIMAL(10, 2) DEFAULT 0,
    subtotal DECIMAL(10, 2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX idx_users_email ON users(email) WHERE deleted_at IS NULL;
CREATE INDEX idx_addresses_user_id ON addresses(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_sku ON products(sku) WHERE deleted_at IS NULL;
CREATE INDEX idx_orders_user_id ON orders(user_id) WHERE deleted_at IS NULL;
";
        await connection.ExecuteAsync(setupScript);
        
        Console.WriteLine("Database setup completed!");
    }
    
    static async Task RunTestsAsync(IDbConnection connection)
    {
        Console.WriteLine("\n=== Starting Rymote.Radiant Tests ===\n");
        
        // Test 1: Create Users
        Console.WriteLine("1. Creating users...");
        User user1 = await CreateUserAsync("john.doe@example.com", "johndoe", "John Doe");
        User user2 = await CreateUserAsync("jane.smith@example.com", "janesmith", "Jane Smith");
        User user3 = await CreateUserAsync("bob.wilson@example.com", "bobwilson", "Bob Wilson");
        Console.WriteLine($"   Created {3} users");
        
        // Test 2: Create Addresses
        Console.WriteLine("\n2. Creating addresses...");
        Address address1 = await CreateAddressAsync(user1.Id, "home", "123 Main St", "New York", "NY", "USA", "10001");
        Address address2 = await CreateAddressAsync(user1.Id, "work", "456 Broadway", "New York", "NY", "USA", "10002");
        Address address3 = await CreateAddressAsync(user2.Id, "home", "789 Park Ave", "Los Angeles", "CA", "USA", "90001");
        Console.WriteLine($"   Created {3} addresses");
        
        // Test 3: Create Categories with hierarchy
        Console.WriteLine("\n3. Creating categories...");
        Category electronics = await CreateCategoryAsync("Electronics", "electronics", null);
        Category computers = await CreateCategoryAsync("Computers", "computers", electronics.Id);
        Category laptops = await CreateCategoryAsync("Laptops", "laptops", computers.Id);
        Category smartphones = await CreateCategoryAsync("Smartphones", "smartphones", electronics.Id);
        Console.WriteLine($"   Created {4} categories with hierarchy");
        
        // Test 4: Create Products
        Console.WriteLine("\n4. Creating products...");
        Product laptop1 = await CreateProductAsync("LAPTOP001", "Gaming Laptop Pro", 1299.99m, 10, new[] { "gaming", "laptop", "high-performance" });
        Product laptop2 = await CreateProductAsync("LAPTOP002", "Business Ultrabook", 899.99m, 25, new[] { "business", "laptop", "lightweight" });
        Product phone1 = await CreateProductAsync("PHONE001", "Smartphone X", 699.99m, 50, new[] { "smartphone", "android", "5G" });
        Console.WriteLine($"   Created {3} products");
        
        // Test 5: Query with relationships
        Console.WriteLine("\n5. Testing relationship loading...");
        User userWithAddresses = await User.Query()
            .Include(u => u.Addresses)
            .Where(u => u.Id == user1.Id)
            .FirstAsync();
        Console.WriteLine($"   User {userWithAddresses.Username} has {userWithAddresses.Addresses.Count} addresses loaded via Include");
        
        // Test 6: Create Orders
        Console.WriteLine("\n6. Creating orders...");
        Order order1 = await CreateOrderAsync(user1.Id, address1.Id);
        await CreateOrderItemAsync(order1.Id, laptop1.Id, 1, laptop1.Price);
        await CreateOrderItemAsync(order1.Id, phone1.Id, 2, phone1.Price);
        
        // Update order total
        order1.TotalAmount = laptop1.Price + (phone1.Price * 2);
        await order1.SaveAsync();
        Console.WriteLine($"   Created order {order1.OrderNumber} with 2 items");
        
        // Test 7: Complex queries
        Console.WriteLine("\n7. Testing complex queries...");
        
        // Query with multiple conditions
        List<User> activeUsers = await User.Query()
            .Where(u => u.IsActive == true)
            .Where(u => u.CreatedAt > DateTime.UtcNow.AddDays(-1))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        Console.WriteLine($"   Found {activeUsers.Count} active users created today");
        
        // Test 8: Full-text search (if products have descriptions)
        Console.WriteLine("\n8. Testing full-text search...");
        List<Product> searchResults = await Product.Query()
            .WhereFullTextSearch(p => p.Name, "laptop")
            .ToListAsync();
        Console.WriteLine($"   Found {searchResults.Count} products matching 'laptop'");
        
        // Test 9: JSONB queries
        Console.WriteLine("\n9. Testing JSONB queries...");
        user1.ProfileData = "{\"preferences\": {\"theme\": \"dark\", \"language\": \"en\"}}";
        await user1.SaveAsync();
        
        List<User> usersWithDarkTheme = await User.Query()
            .WhereJsonbContains(u => u.ProfileData, new { preferences = new { theme = "dark" } })
            .ToListAsync();
        Console.WriteLine($"   Found {usersWithDarkTheme.Count} users with dark theme preference");
        
        // Test 10: Array operations
        Console.WriteLine("\n10. Testing array operations...");
        List<Product> gamingProducts = await Product.Query()
            .WhereArrayContains(p => p.Tags, new[] { "gaming" })
            .ToListAsync();
        Console.WriteLine($"   Found {gamingProducts.Count} gaming products");
        
        // Test 11: Aggregations
        Console.WriteLine("\n11. Testing aggregations...");
        double maxPrice = await Product.Query().MaxAsync(p => (double)p.Price);
        double avgPrice = await Product.Query().AverageAsync(p => (double)p.Price);
        decimal totalStock = await Product.Query().SumAsync(p => p.StockQuantity);
        Console.WriteLine($"   Max price: ${maxPrice:F2}, Avg price: ${avgPrice:F2}, Total stock: {totalStock:F2}");
        
        // Test 12: Raw SQL
        Console.WriteLine("\n12. Testing raw SQL queries...");
        dynamic orderStats = await Order.Raw().QuerySingleAsync<dynamic>(@"
            SELECT 
                COUNT(*) as total_orders,
                SUM(total_amount) as total_revenue,
                AVG(total_amount) as average_order_value
            FROM orders
            WHERE deleted_at IS NULL
        ");
        Console.WriteLine($"   Orders: {orderStats.total_orders}, Revenue: ${orderStats.total_revenue:F2}");
        
        // Test 13: Soft delete
        Console.WriteLine("\n13. Testing soft delete...");
        await user3.DeleteAsync();
        int userCountWithDeleted = await User.Query().WithTrashed().CountAsync();
        int userCountWithoutDeleted = await User.Query().CountAsync();
        Console.WriteLine($"   Users with deleted: {userCountWithDeleted}, without deleted: {userCountWithoutDeleted}");
        
        // Test 14: SQL Builder directly  
        Console.WriteLine("\n14. Testing SQL Builder...");
        
        // Simple query first
        SelectBuilder simpleQuery = new SelectBuilder()
            .Select(
                new ColumnExpression("id"),
                new ColumnExpression("username"),
                new ColumnExpression("email")
            )
            .From("users")
            .WhereNull("deleted_at")
            .OrderBy("created_at", Rymote.Radiant.Sql.Clauses.OrderBy.SortDirection.Descending)
            .Limit(10);
            
        QueryExecutor executor = new QueryExecutor(connection);
        dynamic[] users = (await executor.QueryAsync<dynamic>(simpleQuery.Build())).ToArray();
        Console.WriteLine($"   Found {users.Length} users");
        
        // Test with aggregate
        SelectBuilder countQuery = new SelectBuilder()
            .Select(new FunctionExpression("COUNT", new RawSqlExpression("*")).As("total"))
            .From("orders");
            
        dynamic countResult = await executor.QuerySingleAsync<dynamic>(countQuery.Build());
        Console.WriteLine($"   Total orders: {countResult.total}");
        
        // Test 15: Category hierarchy with CTE
        Console.WriteLine("\n15. Testing recursive CTE for category hierarchy...");
        string categoryHierarchyQuery = @"
            WITH RECURSIVE category_tree AS (
                SELECT id, name, parent_id, 0 as level, name::text as path
                FROM categories
                WHERE parent_id IS NULL AND deleted_at IS NULL
                
                UNION ALL
                
                SELECT c.id, c.name, c.parent_id, ct.level + 1, 
                       ct.path || ' > ' || c.name
                FROM categories c
                INNER JOIN category_tree ct ON c.parent_id = ct.id
                WHERE c.deleted_at IS NULL
            )
            SELECT * FROM category_tree ORDER BY path";
            
        List<dynamic> categoryTree = await Category.Raw().QueryAsync<dynamic>(categoryHierarchyQuery);
        foreach (dynamic category in categoryTree)
        {
            Console.WriteLine($"   {new string(' ', category.level * 2)}- {category.name} (Level {category.level})");
        }
    }
    
    // Helper methods
    static async Task<User> CreateUserAsync(string email, string username, string fullName)
    {
        User user = new User
        {
            Email = email,
            Username = username,
            FullName = fullName,
            IsActive = true,
            Tags = new[] { "new-user", "verified" }
        };
        Console.WriteLine($"   Before create: User.Id = {user.Id}");
        User createdUser = await User.CreateAsync(user);
        Console.WriteLine($"   After create: User.Id = {createdUser.Id}");
        return createdUser;
    }
    
    static async Task<Address> CreateAddressAsync(int userId, string type, string street, string city, string state, string country, string postalCode)
    {
        Console.WriteLine($"   Creating address with UserId = {userId}");
        Address address = new Address
        {
            UserId = userId,
            Type = type,
            Street = street,
            City = city,
            State = state,
            Country = country,
            PostalCode = postalCode,
            IsPrimary = type == "home"
        };
        Console.WriteLine($"   Before save: Address.UserId = {address.UserId}");
        await address.SaveAsync();
        Console.WriteLine($"   After save: Address.UserId = {address.UserId}");
        return address;
    }
    
    static async Task<Category> CreateCategoryAsync(string name, string slug, int? parentId)
    {
        Category category = new Category
        {
            Name = name,
            Slug = slug,
            ParentId = parentId,
            Metadata = "{\"display_in_menu\": true}"
        };
        await category.SaveAsync();
        return category;
    }
    
    static async Task<Product> CreateProductAsync(string sku, string name, decimal price, int stock, string[] tags)
    {
        Product product = new Product
        {
            Sku = sku,
            Name = name,
            Description = $"High-quality {name.ToLower()} with amazing features",
            Price = price,
            StockQuantity = stock,
            Tags = tags,
            Attributes = "{\"warranty\": \"2 years\", \"color\": \"black\"}"
        };
        await product.SaveAsync();
        return product;
    }
    
    static async Task<Order> CreateOrderAsync(int userId, int? shippingAddressId)
    {
        Order order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            UserId = userId,
            ShippingAddressId = shippingAddressId,
            Status = "pending",
            PlacedAt = DateTime.UtcNow
        };
        await order.SaveAsync();
        return order;
    }
    
    static async Task<OrderItem> CreateOrderItemAsync(int orderId, int productId, int quantity, decimal unitPrice)
    {
        OrderItem item = new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = quantity * unitPrice
        };
        await item.SaveAsync();
        return item;
    }
}