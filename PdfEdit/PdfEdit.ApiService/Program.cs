using PdfEdit.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add MVC controllers for PDF endpoints
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// PDF processing service
builder.Services.AddSingleton<IPdfService, PdfService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
