using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using TradeOps.Core.Data;
using TradeOps.Core.Models;
using TradeOps.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers and OpenAPI Docs
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Get PostgreSQL connection string
var pgConnectionString = builder.Configuration.GetConnectionString("Postgres");

// Register EF Core + PostgreSQL repository if configured, otherwise fall back to InMemory
if (!string.IsNullOrEmpty(pgConnectionString))
{
    var nameTranslator = new NpgsqlNullNameTranslator();
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(pgConnectionString);
    dataSourceBuilder.MapEnum<OrderSide>("order_side", nameTranslator: nameTranslator);
    dataSourceBuilder.MapEnum<TradeStatus>("trade_status", nameTranslator: nameTranslator);
    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddSingleton(dataSource);
    builder.Services.AddDbContext<TradeOpsDbContext>(options => options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.MapEnum<OrderSide>("order_side", nameTranslator: nameTranslator);
        npgsqlOptions.MapEnum<TradeStatus>("trade_status", nameTranslator: nameTranslator);
    }));

    // Scoped: EF Core's DbContext is not thread-safe and must not be shared across concurrent requests.
    builder.Services.AddScoped<ITradeRepository, PostgresTradeRepository>();
    builder.Services.AddScoped<IUserService, PostgresUserService>();
}
else
{
    builder.Services.AddSingleton<ITradeRepository, InMemoryTradeRepository>();
    builder.Services.AddSingleton<IUserService, InMemoryUserService>();
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
