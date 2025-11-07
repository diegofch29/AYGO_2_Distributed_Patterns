
using LoadBalancer.Helper;
using LoadBalancer.Services;
using LoadBalancer.Hubs;
using LoadBalancer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    options.Configuration = connectionString;

    // Add connection resilience options
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { connectionString ?? "localhost:6379" },
        ConnectTimeout = 10000, // 10 seconds
        SyncTimeout = 5000,     // 5 seconds
        ConnectRetry = 3,
        ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(1000),
        AbortOnConnectFail = false,
        AllowAdmin = false
    };
});

var signalRBuilder = builder.Services.AddSignalR();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

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

builder.Services.AddHostedService<SignalRReplicationService>();


// Register HttpClient and HttpClientHelper
builder.Services.AddHttpClient<IHttpClientHelper, HttpClientHelper>();

// Register custom services
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ILogNotificationService, LogNotificationService>();
builder.Services.AddHostedService<RedisHealthService>();

var app = builder.Build();

app.MapHub<ReplicationHub>("/replicate");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();