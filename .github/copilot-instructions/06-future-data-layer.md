# Future Data Layer - Agentic Data Access Patterns

## Project Structure (When Adding Data Layer)
```
src/
├── MafEvaluation.Data/
│   ├── Agents/              # Data-aware agents
│   ├── Repositories/        # Repository implementations
│   ├── Entities/            # Entity models
│   ├── Tools/               # Data access tools for agents
│   └── Program.cs           # Data layer initialization
```

## Agentic Data Patterns

### Pattern 1: Agent-Powered Repository
Agent uses tools to query and manipulate data intelligently.

```csharp
public interface ICustomerAgent
{
    Task<CustomerResponse> SearchCustomersAsync(string naturalLanguageQuery);
    Task<CustomerResponse> UpdateCustomerAsync(string instructions);
}

public class CustomerAgentRepository : ICustomerAgent
{
    private readonly AIAgent _agent;
    private readonly ICustomerRepository _repository;
    
    public CustomerAgentRepository(IChatClient chatClient, ICustomerRepository repository)
    {
        _repository = repository;
        
        _agent = chatClient.AsIChatClient().CreateAIAgent(
            instructions: @"You are a customer data assistant. 
                Use the provided tools to search, retrieve, and update customer information.
                Always validate data before making changes.",
            name: "CustomerDataAgent",
            tools:
            [
                AIFunctionFactory.Create(SearchCustomers),
                AIFunctionFactory.Create(GetCustomerById),
                AIFunctionFactory.Create(UpdateCustomer),
                AIFunctionFactory.Create(CreateCustomer)
            ]
        );
    }
    
    // Tool implementations
    private async Task<string> SearchCustomers(string query, string? city = null, string? status = null)
    {
        var customers = await _repository.SearchAsync(query, city, status);
        return JsonSerializer.Serialize(customers);
    }
    
    private async Task<string> GetCustomerById(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        return JsonSerializer.Serialize(customer);
    }
    
    private async Task<string> UpdateCustomer(int id, string? name, string? email, string? phone)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null) return "Customer not found";
        
        // Update fields if provided
        if (name != null) customer.Name = name;
        if (email != null) customer.Email = email;
        if (phone != null) customer.Phone = phone;
        
        await _repository.UpdateAsync(customer);
        return $"Customer {id} updated successfully";
    }
    
    public async Task<CustomerResponse> SearchCustomersAsync(string naturalLanguageQuery)
    {
        var executionData = await ExecuteAgentAsync(_agent, naturalLanguageQuery);
        return new CustomerResponse 
        { 
            Response = executionData.Response,
            Data = ExtractDataFromResponse(executionData)
        };
    }
}
```

### Pattern 2: Query Generation Agent
Agent generates SQL/LINQ queries from natural language.

```csharp
public class QueryGenerationAgent
{
    private readonly AIAgent _agent;
    private readonly DbContext _context;
    
    public async Task<IEnumerable<T>> QueryAsync<T>(string naturalLanguageQuery) where T : class
    {
        // Agent generates LINQ expression
        var linqQuery = await _agent.GenerateLinqQueryAsync(naturalLanguageQuery, typeof(T));
        
        // Execute query safely
        var results = await _context.Set<T>()
            .Where(linqQuery)
            .ToListAsync();
            
        return results;
    }
}
```

### Pattern 3: Data Validation Agent
Agent validates data integrity and business rules.

```csharp
public class DataValidationAgent
{
    private readonly AIAgent _agent;
    
    public async Task<ValidationResult> ValidateAsync<T>(T entity, string context)
    {
        var entityJson = JsonSerializer.Serialize(entity);
        
        var prompt = $@"
        Validate the following {typeof(T).Name} entity:
        
        Entity: {entityJson}
        Context: {context}
        
        Check for:
        - Required fields
        - Data format correctness
        - Business rule violations
        - Potential data quality issues
        
        Return validation result with issues found.
        ";
        
        var result = await _agent.QueryAsync(prompt);
        return ParseValidationResult(result);
    }
}
```

## Database Configuration with Aspire

### PostgreSQL
```csharp
// AppHost
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("mafdb");

var dataApi = builder.AddProject<Projects.MafEvaluation_DataApi>("data-api")
    .WithReference(postgres);
```

### SQL Server
```csharp
var sqlServer = builder.AddSqlServer("sql")
    .AddDatabase("mafdb");
```

### Entity Framework Setup
```csharp
builder.AddNpgsqlDbContext<AppDbContext>("mafdb");
// or
builder.AddSqlServerDbContext<AppDbContext>("mafdb");
```

## Repository with Agent Tools

```csharp
public interface IProductRepository
{
    Task<IEnumerable<Product>> SearchAsync(string query);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    
    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Product>> SearchAsync(string query)
    {
        return await _context.Products
            .Where(p => EF.Functions.Like(p.Name, $"%{query}%") ||
                       EF.Functions.Like(p.Description, $"%{query}%"))
            .ToListAsync();
    }
    
    // Other CRUD operations
}
```

## Agent Tools for Data Access

```csharp
public class ProductDataTools
{
    private readonly IProductRepository _repository;
    
    public ProductDataTools(IProductRepository repository)
    {
        _repository = repository;
    }
    
    [Description("Search for products by name or description")]
    public async Task<string> SearchProducts(
        [Description("Search query")] string query,
        [Description("Minimum price")] decimal? minPrice = null,
        [Description("Maximum price")] decimal? maxPrice = null)
    {
        var products = await _repository.SearchAsync(query);
        
        if (minPrice.HasValue)
            products = products.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            products = products.Where(p => p.Price <= maxPrice.Value);
        
        return JsonSerializer.Serialize(products.Take(10));
    }
    
    [Description("Get detailed product information by ID")]
    public async Task<string> GetProductDetails(
        [Description("Product ID")] int productId)
    {
        var product = await _repository.GetByIdAsync(productId);
        return product != null 
            ? JsonSerializer.Serialize(product)
            : "Product not found";
    }
    
    [Description("Update product information")]
    public async Task<string> UpdateProduct(
        [Description("Product ID")] int productId,
        [Description("New name")] string? name = null,
        [Description("New price")] decimal? price = null,
        [Description("New description")] string? description = null)
    {
        var product = await _repository.GetByIdAsync(productId);
        if (product == null) return "Product not found";
        
        if (name != null) product.Name = name;
        if (price.HasValue) product.Price = price.Value;
        if (description != null) product.Description = description;
        
        await _repository.UpdateAsync(product);
        return $"Product {productId} updated successfully";
    }
}
```

## Creating Data Agent

```csharp
public static class DataAgentFactory
{
    public static AIAgent CreateProductAgent(
        IChatClient chatClient, 
        IProductRepository repository)
    {
        var tools = new ProductDataTools(repository);
        
        return chatClient.AsIChatClient().CreateAIAgent(
            instructions: @"You are a product data assistant.
                Help users search for products, get product details, and update product information.
                Always confirm before making data changes.
                Provide clear, structured responses about products.",
            name: "ProductDataAgent",
            tools:
            [
                AIFunctionFactory.Create(tools.SearchProducts),
                AIFunctionFactory.Create(tools.GetProductDetails),
                AIFunctionFactory.Create(tools.UpdateProduct)
            ]
        );
    }
}
```

## Agentic RAG (Retrieval-Augmented Generation)

```csharp
public class DocumentAgent
{
    private readonly AIAgent _agent;
    private readonly IVectorStore _vectorStore;
    
    public async Task<string> AnswerQuestionAsync(string question)
    {
        // Retrieve relevant documents
        var relevantDocs = await _vectorStore.SearchAsync(question, topK: 5);
        var context = string.Join("\n\n", relevantDocs.Select(d => d.Content));
        
        // Agent answers using retrieved context
        var prompt = $@"
        Answer the following question using ONLY the provided context.
        If the context doesn't contain the answer, say so.
        
        Question: {question}
        
        Context:
        {context}
        ";
        
        return await _agent.QueryAsync(prompt);
    }
}
```

## Transaction Management

```csharp
public class TransactionalDataAgent
{
    private readonly AppDbContext _context;
    private readonly AIAgent _agent;
    
    public async Task<AgentResponse> ExecuteDataOperationAsync(string instruction)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var result = await _agent.QueryAsync(instruction);
            
            // Verify changes are acceptable
            if (await ValidateChangesAsync())
            {
                await transaction.CommitAsync();
                return result;
            }
            else
            {
                await transaction.RollbackAsync();
                return new AgentResponse { Error = "Validation failed" };
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

## Best Practices
- **Never allow direct SQL execution** from agent-generated queries
- **Always validate** data changes before committing
- **Use transactions** for multi-step operations
- **Implement audit logging** for agent-initiated data changes
- **Limit tool permissions** based on user roles
- **Sanitize inputs** before passing to database
- **Use parameterized queries** to prevent injection
- **Implement retry logic** for transient failures
- **Monitor agent data access** patterns via telemetry
- **Test with diverse queries** to ensure safety
- **Document tool capabilities** clearly in agent instructions
