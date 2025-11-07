using LogApi.Stores;
using LogApi.Hubs;
using LogApi.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add SignalR with optional Redis backplane for multi-instance support
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();

if (!string.IsNullOrEmpty(redisConnectionString) && redisConnectionString != "0.0.0.0")
{
    try
    {
        signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("LogApi");
            options.Configuration.AbortOnConnectFail = false;
            options.Configuration.ConnectTimeout = 5000;
            options.Configuration.SyncTimeout = 5000;
        });

        // Test Redis connection
        var redisConfig = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        redisConfig.AbortOnConnectFail = false;
        redisConfig.ConnectTimeout = 5000;
        using var connection = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig);

        Console.WriteLine($"Successfully connected to Redis at {redisConnectionString}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to connect to Redis at {redisConnectionString}. Running in single-instance mode. Error: {ex.Message}");
    }
}
else
{
    Console.WriteLine("Redis connection string not configured or set to 0.0.0.0. Running SignalR in single-instance mode.");
}

builder.Services.AddSingleton<KeyValueStore>();
builder.Services.AddSingleton<LogApi.Services.ILogService, LogApi.Services.LogService>();

// Register HTTP client for service registration with timeout configuration
builder.Services.AddHttpClient<IServiceRegistrationService, ServiceRegistrationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout
});

// Register service registration services
builder.Services.AddSingleton<IServiceRegistrationService, ServiceRegistrationService>();
builder.Services.AddHostedService<ServiceRegistrationHostedService>();

// Register SignalR replication service for cross-VM sync
builder.Services.AddHostedService<SignalRReplicationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<ReplicationHub>("/replicate");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
