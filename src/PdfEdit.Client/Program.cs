using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PdfEdit.Client;
using PdfEdit.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API communication.
// When hosted by the API, the base address is the host's address.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<IPdfApiService, PdfApiService>();

await builder.Build().RunAsync();
// Register services
builder.Services.AddScoped<IPdfApiService, PdfApiService>();

await builder.Build().RunAsync();
