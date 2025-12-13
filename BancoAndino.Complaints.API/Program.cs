using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using BancoAndino.Complaints.Shared.Data;
using BancoAndino.Complaints.Shared.Interfaces;
using BancoAndino.Complaints.API.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure Entity Framework
var sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=BancoAndinoComplaints;Trusted_Connection=True;";

builder.Services.AddDbContext<ComplaintsDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Configure Services
var redisConnectionString = Environment.GetEnvironmentVariable("RedisConnectionString") ?? "localhost:6379";
var blobStorageConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString") ?? "UseDevelopmentStorage=true";

builder.Services.AddScoped<IComplaintRepository>(sp =>
{
    var context = sp.GetRequiredService<ComplaintsDbContext>();
    return new ComplaintRepository(context, sqlConnectionString);
});

builder.Services.AddSingleton<IStorageService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<StorageService>>();
    return new StorageService(blobStorageConnectionString, logger);
});

// Register Redis ConnectionMultiplexer as singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddSingleton<ICacheService, CacheService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Serilog logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSerilog(dispose: true);
});

builder.Build().Run();
