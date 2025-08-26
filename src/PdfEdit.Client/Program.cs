using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PdfEdit.Client;
using PdfEdit.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API communication
// When hosted as static files in the API, use relative URLs
var apiBaseUrl = builder.HostEnvironment.IsDevelopment() 
    ? "https://localhost:7127" 
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register services
builder.Services.AddScoped<IPdfApiService, PdfApiService>();

await builder.Build().RunAsync();
