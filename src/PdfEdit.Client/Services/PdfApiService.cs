using PdfEdit.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace PdfEdit.Client.Services;

public interface IPdfApiService
{
    Task<PdfUploadResponse?> UploadPdfAsync(IBrowserFile file);
    Task<HttpResponseMessage> ProcessPdfAsync(PdfEditRequest request);
}

public class PdfApiService : IPdfApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfApiService> _logger;

    public PdfApiService(HttpClient httpClient, ILogger<PdfApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PdfUploadResponse?> UploadPdfAsync(IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)); // 50MB limit
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync("api/pdf/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PdfUploadResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                _logger.LogError("Failed to upload PDF. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF file: {FileName}", file.Name);
            return null;
        }
    }

    public async Task<HttpResponseMessage> ProcessPdfAsync(PdfEditRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/pdf/process", request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF for document: {DocumentId}", request.DocumentId);
            throw;
        }
    }
}