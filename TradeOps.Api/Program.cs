using TradeOps.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers and OpenAPI Docs
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Get PostgreSQL connection string
var pgConnectionString = builder.Configuration.GetConnectionString("Postgres");

// Register PostgreSQL repository if configured, otherwise fall back to InMemory
if (!string.IsNullOrEmpty(pgConnectionString))
{
    builder.Services.AddSingleton<ITradeRepository>(new PostgresTradeRepository(pgConnectionString));
}
else
{
    builder.Services.AddSingleton<ITradeRepository, InMemoryTradeRepository>();
}

builder.Services.AddSingleton<ITradeProcessingQueue, ChannelTradeProcessingQueue>();
builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddHostedService<TradeExecutionWorker>();

// Enable CORS for Back-Office Dashboard (React/Frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
