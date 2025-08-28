using Microsoft.AspNetCore.Mvc;
using PdfEdit.Api.Services;
using PdfEdit.Shared.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PdfEdit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfService _pdfService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(IPdfService pdfService, ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only PDF files are allowed");
            }

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
            {
                return BadRequest("File size exceeds maximum limit of 50MB");
            }

            using var stream = file.OpenReadStream();
            var response = await _pdfService.ExtractFormFieldsAsync(stream, file.FileName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF file: {FileName}", file?.FileName);
            return StatusCode(500, "An error occurred while processing the file");
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPdf([FromBody] PdfEditRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.DocumentId) && string.IsNullOrEmpty(request.OriginalPdfBase64))
            {
                return BadRequest("Either DocumentId or OriginalPdfBase64 is required");
            }

            var processedPdf = await _pdfService.ProcessPdfAsync(request);
            return File(processedPdf, "application/pdf", $"edited-document-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Document not found: {DocumentId}", request.DocumentId);
            return NotFound("Document not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF for document: {DocumentId}", request.DocumentId);
            return StatusCode(500, "An error occurred while processing the PDF");
        }
    }

    [HttpDelete("{documentId}")]
    public ActionResult CleanupDocument(string documentId)
    {
        try
        {
            _pdfService.CleanupDocument(documentId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up document: {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while cleaning up the document");
        }
    }

    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetOriginal(string documentId)
    {
        try
        {
            var bytes = await _pdfService.GetDocumentAsync(documentId);
            return File(bytes, "application/pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}