
using LoadBalancer.Helper;
using LoadBalancer.Services;

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

// Register HttpClient and HttpClientHelper
builder.Services.AddHttpClient<IHttpClientHelper, HttpClientHelper>();

// Register custom services
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddHostedService<RedisHealthService>();

var app = builder.Build();

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