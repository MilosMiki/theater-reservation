using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using DotNetEnv;
using Supabase;
using Microsoft.OpenApi.Models;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Enable console logging
builder.Logging.AddConsole();

// Load the .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
DotNetEnv.Env.Load(envPath);

// Set appUrl
var appUrl = Environment.GetEnvironmentVariable("DOTNET_URL") ?? "http://localhost:5000";
builder.WebHost.UseUrls(appUrl);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Reservations API",
        Version = "v1",
        Description = "API for managing theater reservations"
    });
});

// Supabase client
var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? 
        throw new ArgumentNullException("SUPABASE_URL is not set");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? 
        throw new ArgumentNullException("SUPABASE_KEY is not set");
var supabase = new Supabase.Client(url, key);
await supabase.InitializeAsync();

// Kafka configuration
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "localhost:9093";

// Configure Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageSendMaxRetries = 3,
        RetryBackoffMs = 1000
    };
    return new ProducerBuilder<string, string>(config)
        .SetLogHandler((_, message) =>
            sp.GetRequiredService<ILogger<Program>>().LogInformation("Kafka Producer: {Facility} {Message}", message.Facility, message.Message))
        .Build();
});

// Configure Kafka Consumer
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = "reservation-service-group",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
        IsolationLevel = IsolationLevel.ReadCommitted
    };
    return new ConsumerBuilder<string, string>(config)
        .SetLogHandler((_, message) =>
            sp.GetRequiredService<ILogger<Program>>().LogInformation("Kafka Consumer: {Facility} {Message}", message.Facility, message.Message))
        .Build();
});

// DI setup
builder.Services.AddSingleton(supabase);
builder.Services.AddScoped<IReservationRepository, SupabaseReservationRepository>();
builder.Services.AddScoped<ReservationService>();

var app = builder.Build();

// Exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (NotSupportedException ex)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("A NotSupportedException occurred. This is okay, this is just .net implementing Supabase wrong..");
    }
    catch (KafkaException ex)
    {
        app.Logger.LogError(ex, "Kafka communication error");
        context.Response.StatusCode = 503;
        await context.Response.WriteAsync("Service unavailable - Kafka communication error");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal server error");
    }
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reservations API v1");
    c.RoutePrefix = string.Empty;
});

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("Service is healthy"));

// Kafka initialization test
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var producer = scope.ServiceProvider.GetRequiredService<IProducer<string, string>>();
    
    try
    {
        var testMessage = new Message<string, string>
        {
            Key = "health-check",
            Value = "Service started at " + DateTime.UtcNow.ToString("O")
        };
        
        await producer.ProduceAsync("health-checks", testMessage);
        logger.LogInformation("Successfully connected to Kafka and sent health check message");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to Kafka");
        // Don't crash the app, but log the error
    }
}

app.Run();