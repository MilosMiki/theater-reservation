using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using DotNetEnv;
using Supabase;
using Microsoft.OpenApi.Models;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Omogoči logiranje v konzolo
builder.Logging.AddConsole();

// Load the .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
DotNetEnv.Env.Load(envPath);

// Set appUrl
var appUrl = Environment.GetEnvironmentVariable("DOTNET_URL") ?? "http://localhost:5000";
builder.WebHost.UseUrls(appUrl);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Supabase client
var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? 
        throw new ArgumentNullException("SUPABASE_URL is not set");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? 
        throw new ArgumentNullException("SUPABASE_KEY is not set");
var supabase = new Supabase.Client(url, key);
await supabase.InitializeAsync();

// DI setup
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "localhost:9093";
builder.Services.AddSingleton(supabase);
builder.Services.AddScoped<IReservationRepository, SupabaseReservationRepository>();
builder.Services.AddSingleton<KafkaProducer>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<KafkaProducer>>();
    return new KafkaProducer(bootstrapServers, logger);
});
// builder.Services.AddSingleton(new KafkaProducer("localhost:9092"));
builder.Services.AddScoped<ReservationService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (NotSupportedException ex)
    {
        // Handle NotSupportedException globally
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("A NotSupportedException occurred. This is okay!");
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

using (var scope = app.Services.CreateScope()) // test Kafka connection
{
    var kafkaProducer = scope.ServiceProvider.GetRequiredService<KafkaProducer>();
    await kafkaProducer.PublishAsync("test_topic", new { Message = "Test message to trigger Kafka logging" });
}

app.Run();
