using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using WeatherApi.Dto;
using WeatherApi.Middleware;
using WeatherApi.Models;
using WeatherApi.Services;
using WeatherApi.Validators;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Logging already configured by default
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Weather API", Version = "v1" });
});

builder.Services.Configure<OpenWeatherMapSettings>(
    builder.Configuration.GetSection("OpenWeatherMap"));

// HttpClientFactory for OpenWeatherMap
builder.Services.AddHttpClient("OpenWeather", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenWeatherMap:BaseUrl"]);
    //client.Timeout = TimeSpan.FromSeconds(10); // avoid hanging requests
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

// Configure Serilog

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/weather-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // Retry for transient 5xx or 408 responses, with exponential backoff
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    // Timeout individual HTTP calls after 10 seconds
    return Policy.TimeoutAsync<HttpResponseMessage>(10);
}


// Memory cache for bonus caching
builder.Services.AddMemoryCache();

// Register services
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();

// Register validators
builder.Services.AddScoped<IValidator<WeatherQueryDto>, WeatherQueryValidator>();
builder.Services.AddScoped<IValidator<UserPreferenceDto>, UserPreferenceValidator>();

var app = builder.Build();

// Global error handling middleware
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.MapControllers();

app.Run();
