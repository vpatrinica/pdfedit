using PdfEdit.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register PDF service
builder.Services.AddScoped<IPdfService, PdfService>();

// Configure CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:5104", "https://localhost:7127", "http://localhost:5148")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("BlazorPolicy");

// Serve static files (for Blazor WASM)
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Fallback to serve Blazor WASM app for client-side routing
app.MapFallbackToFile("index.html");

app.Run();
