using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using PdfEdit.Shared.Models;
using System.Collections.Concurrent;

namespace PdfEdit.Api.Services;

public interface IPdfService
{
    Task<PdfUploadResponse> UploadPdfAsync(Stream pdfStream, string fileName);
    Task<byte[]> ProcessPdfAsync(PdfEditRequest request);
    void CleanupDocument(string documentId);
}

public class PdfService : IPdfService
{
    private readonly ConcurrentDictionary<string, byte[]> _documents = new();
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfUploadResponse> UploadPdfAsync(Stream pdfStream, string fileName)
    {
        try
        {
            var documentId = Guid.NewGuid().ToString();
            
            // Read the PDF stream into memory
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            var pdfBytes = memoryStream.ToArray();
            
            // Store the original PDF
            _documents[documentId] = pdfBytes;

            // Analyze the PDF to extract form fields
            var response = new PdfUploadResponse
            {
                Id = documentId,
                FileName = fileName
            };

            using var reader = new PdfReader(new MemoryStream(pdfBytes));
            using var pdfDoc = new PdfDocument(reader);
            
            response.PageCount = pdfDoc.GetNumberOfPages();
            
            // Extract existing form fields
            var form = PdfFormCreator.GetAcroForm(pdfDoc, false);
            if (form != null)
            {
                var fields = form.GetAllFormFields();
                foreach (var field in fields)
                {
                    var formField = ConvertToFormField(field.Key, field.Value);
                    if (formField != null)
                    {
                        response.FormFields.Add(formField);
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF upload for file: {FileName}", fileName);
            throw new InvalidOperationException("Failed to process PDF file", ex);
        }
    }

    public async Task<byte[]> ProcessPdfAsync(PdfEditRequest request)
    {
        try
        {
            if (!_documents.TryGetValue(request.DocumentId, out var originalPdf))
            {
                throw new ArgumentException($"Document not found: {request.DocumentId}");
            }

            using var inputStream = new MemoryStream(originalPdf);
            using var outputStream = new MemoryStream();
            
            using var reader = new PdfReader(inputStream);
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);
            using var document = new Document(pdfDoc);

            // Update existing form fields
            var form = PdfFormCreator.GetAcroForm(pdfDoc, true);
            if (form != null)
            {
                foreach (var field in request.FormFields)
                {
                    var pdfField = form.GetField(field.Name);
                    if (pdfField != null)
                    {
                        pdfField.SetValue(field.Value);
                    }
                }
                
                // Flatten the form to make it non-editable
                form.FlattenFields();
            }

            // Add new text elements
            foreach (var textElement in request.TextElements)
            {
                AddTextElement(document, pdfDoc, textElement);
            }

            // Add new image elements (signatures)
            foreach (var imageElement in request.ImageElements)
            {
                AddImageElement(document, pdfDoc, imageElement);
            }

            document.Close();
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF for document: {DocumentId}", request.DocumentId);
            throw new InvalidOperationException("Failed to process PDF modifications", ex);
        }
    }

    public void CleanupDocument(string documentId)
    {
        _documents.TryRemove(documentId, out _);
    }

    private PdfEdit.Shared.Models.PdfFormField? ConvertToFormField(string name, iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var bounds = GetFieldBounds(field);
            var fieldType = GetFieldType(field);
            
            return new PdfEdit.Shared.Models.PdfFormField
            {
                Name = name,
                Type = fieldType,
                Value = field.GetValueAsString() ?? string.Empty,
                IsRequired = field.IsRequired(),
                PageNumber = GetFieldPageNumber(field),
                Bounds = bounds
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert form field: {FieldName}", name);
            return null;
        }
    }

    private PdfEdit.Shared.Models.Rectangle GetFieldBounds(iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var widgets = field.GetWidgets();
            if (widgets.Count > 0)
            {
                var rectArray = widgets[0].GetRectangle();
                if (rectArray != null && rectArray.Size() >= 4)
                {
                    return new PdfEdit.Shared.Models.Rectangle
                    {
                        X = rectArray.GetAsNumber(0)?.DoubleValue() ?? 0,
                        Y = rectArray.GetAsNumber(1)?.DoubleValue() ?? 0,
                        Width = (rectArray.GetAsNumber(2)?.DoubleValue() ?? 0) - (rectArray.GetAsNumber(0)?.DoubleValue() ?? 0),
                        Height = (rectArray.GetAsNumber(3)?.DoubleValue() ?? 0) - (rectArray.GetAsNumber(1)?.DoubleValue() ?? 0)
                    };
                }
            }
        }
        catch { }
        
        return new PdfEdit.Shared.Models.Rectangle();
    }

    private PdfFieldType GetFieldType(iText.Forms.Fields.PdfFormField field)
    {
        var fieldType = field.GetFormType();
        if (fieldType.Equals(PdfName.Tx))
            return PdfFieldType.Text;
        else if (fieldType.Equals(PdfName.Btn))
            return PdfFieldType.Checkbox; // Simplified - treat all buttons as checkboxes for now
        else if (fieldType.Equals(PdfName.Ch))
            return PdfFieldType.ComboBox;
        else if (fieldType.Equals(PdfName.Sig))
            return PdfFieldType.Signature;
        
        return PdfFieldType.Text;
    }

    private int GetFieldPageNumber(iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var widgets = field.GetWidgets();
            if (widgets.Count > 0)
            {
                var page = widgets[0].GetPage();
                if (page != null)
                {
                    return page.GetDocument().GetPageNumber(page);
                }
            }
        }
        catch { }
        
        return 1;
    }

    private void AddTextElement(Document document, PdfDocument pdfDoc, PdfTextElement textElement)
    {
        try
        {
            var page = pdfDoc.GetPage(textElement.PageNumber);
            
            // Create text with specified properties
            var text = new Paragraph(textElement.Text)
                .SetFontSize(textElement.FontSize)
                .SetFixedPosition(textElement.PageNumber, 
                                (float)textElement.Bounds.X, 
                                (float)textElement.Bounds.Y, 
                                (float)textElement.Bounds.Width);

            document.Add(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add text element: {TextId}", textElement.Id);
        }
    }

    private void AddImageElement(Document document, PdfDocument pdfDoc, PdfImageElement imageElement)
    {
        try
        {
            // Decode base64 image
            var imageBytes = Convert.FromBase64String(imageElement.ImageData);
            var imageData = ImageDataFactory.Create(imageBytes);
            var image = new Image(imageData);

            // Position and size the image
            image.SetFixedPosition(imageElement.PageNumber,
                                 (float)imageElement.Bounds.X,
                                 (float)imageElement.Bounds.Y);
            image.ScaleToFit((float)imageElement.Bounds.Width, (float)imageElement.Bounds.Height);

            document.Add(image);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add image element: {ImageId}", imageElement.Id);
        }
    }
}