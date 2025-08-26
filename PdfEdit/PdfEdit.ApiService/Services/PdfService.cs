using iText.Forms;
using iText.Kernel.Pdf;
using PdfEdit.Shared.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using iText.Forms.Fields;

namespace PdfEdit.Api.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private static readonly ConcurrentDictionary<string, byte[]> _documents = new();

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfUploadResponse> ExtractFormFieldsAsync(Stream pdfStream, string fileName)
    {
        var documentId = Guid.NewGuid().ToString();
        
        using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream);
        var pdfBytes = memoryStream.ToArray();
        
        _documents[documentId] = pdfBytes;

        var response = new PdfUploadResponse
        {
            Id = documentId,
            FileName = fileName
        };

        using var reader = new PdfReader(new MemoryStream(pdfBytes));
        using var pdfDoc = new PdfDocument(reader);
        
        response.PageCount = pdfDoc.GetNumberOfPages();
        
        var form = PdfAcroForm.GetAcroForm(pdfDoc, false);
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

    public async Task<byte[]> ProcessPdfAsync(PdfEditRequest request)
    {
        if (!_documents.TryGetValue(request.DocumentId, out var originalPdf))
        {
            throw new FileNotFoundException("Document not found or expired.", request.DocumentId);
        }

        using var inputStream = new MemoryStream(originalPdf);
        using var outputStream = new MemoryStream();
        
        using var reader = new PdfReader(inputStream);
        using var writer = new PdfWriter(outputStream);
        using var pdfDoc = new PdfDocument(reader, writer);
        using var document = new Document(pdfDoc);

        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        if (form != null)
        {
            foreach (var field in request.FormFields)
            {
                if (form.GetField(field.Name) is var pdfField && pdfField != null)
                {
                    pdfField.SetValue(field.Value);
                }
            }
            
            form.FlattenFields();
        }

        foreach (var textElement in request.TextElements)
        {
            AddTextElement(document, pdfDoc, textElement);
        }

        foreach (var imageElement in request.ImageElements)
        {
            AddImageElement(document, pdfDoc, imageElement);
        }

        document.Close();

        await Task.CompletedTask;
        return outputStream.ToArray();
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

    private Rectangle GetFieldBounds(iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var widgets = field.GetWidgets();
            if (widgets.Count > 0)
            {
                var rectArray = widgets[0].GetRectangle();
                if (rectArray != null && rectArray.Size() >= 4)
                {
                    return new Rectangle
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
        
        return new Rectangle();
    }

    private PdfFieldType GetFieldType(iText.Forms.Fields.PdfFormField field)
    {
        var fieldType = field.GetFormType();
        if (fieldType.Equals(PdfName.Tx))
            return PdfFieldType.Text;
        else if (fieldType.Equals(PdfName.Btn))
            return PdfFieldType.Checkbox;
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
            var imageBytes = Convert.FromBase64String(imageElement.ImageData);
            var imageData = ImageDataFactory.Create(imageBytes);
            var image = new Image(imageData);

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