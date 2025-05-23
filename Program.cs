using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}";
    ConfigurationOptions option = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
        EndPoints = { connectionString }
    };
    return ConnectionMultiplexer.Connect(option);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


// Use the redis connection in a request
app.MapGet("/key/{key}", async (string key, IConnectionMultiplexer redisConnection) =>
{
    var db = redisConnection.GetDatabase();
    var value = await db.StringGetAsync(key);
    
    if (value.IsNull)
        return Results.NotFound(new { key, message = "Key not found" });
        
    return Results.Ok(new { key, value = value.ToString() });
});

app.MapPost("/key", async (KeyValueRequest request, IConnectionMultiplexer redisConnection) =>
{
    var db = redisConnection.GetDatabase();
    db.StringSet(request.Key, request.Value);
    return Results.Ok(new { key = request.Key, value = request.Value, status = "saved" });
});

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


record KeyValueRequest(string Key, string Value);