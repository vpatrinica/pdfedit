using PdfEdit.Shared.Models;
using System.IO;
using System.Threading.Tasks;

namespace PdfEdit.Api.Services;

public interface IPdfService
{
    Task<PdfUploadResponse> ExtractFormFieldsAsync(Stream pdfStream, string fileName);
    Task<byte[]> ProcessPdfAsync(PdfEditRequest request);
    void CleanupDocument(string documentId);
    Task<byte[]> GetDocumentAsync(string documentId);
    Task<byte[]> GetPageAsImageAsync(string documentId, int page);
}
