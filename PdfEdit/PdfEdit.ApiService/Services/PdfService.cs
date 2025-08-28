using System.Collections.Concurrent;
using System.Linq; // added for Select
using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using PdfEdit.Shared.Models;
using RectangleModel = PdfEdit.Shared.Models.Rectangle;
using iText.Kernel.Colors; // added

namespace PdfEdit.Api.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private static readonly ConcurrentDictionary<string, byte[]> _documents = new();
    // 1x1 white JPEG
    private static readonly byte[] PlaceholderJpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAwT/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCfAA//2Q==");

    public PdfService(ILogger<PdfService> logger) => _logger = logger;

    public async Task<PdfUploadResponse> ExtractFormFieldsAsync(Stream pdfStream, string fileName)
    {
        var documentId = Guid.NewGuid().ToString();
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        _documents[documentId] = bytes;

        var response = new PdfUploadResponse { Id = documentId, FileName = fileName };

        using var reader = new PdfReader(new MemoryStream(bytes));
        using var pdfDoc = new PdfDocument(reader);
        response.PageCount = pdfDoc.GetNumberOfPages();

        var form = PdfAcroForm.GetAcroForm(pdfDoc, false);
        if (form != null)
        {
            foreach (var kv in form.GetAllFormFields())
            {
                var converted = ConvertToFormField(kv.Key, kv.Value);
                if (converted != null) response.FormFields.Add(converted);
            }
        }
        return response;
    }

    public async Task<byte[]> ProcessPdfAsync(PdfEditRequest request)
    {
        byte[] originalPdf = !string.IsNullOrEmpty(request.OriginalPdfBase64)
            ? Convert.FromBase64String(request.OriginalPdfBase64)
            : (!string.IsNullOrEmpty(request.DocumentId) && _documents.TryGetValue(request.DocumentId, out var cached)
                ? cached
                : throw new FileNotFoundException("Document not found or expired.", request.DocumentId));

        using var input = new MemoryStream(originalPdf);
        using var output = new MemoryStream();
        using var reader = new PdfReader(input);
        using var writer = new PdfWriter(output);
        using var pdfDoc = new PdfDocument(reader, writer);
        using var doc = new Document(pdfDoc);

        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        if (form != null)
        {
            foreach (var f in request.FormFields)
            {
                var pdfField = form.GetField(f.Name);
                pdfField?.SetValue(f.Value);
            }
            form.FlattenFields();
        }

        foreach (var t in request.TextElements) AddTextElement(doc, pdfDoc, t);
        foreach (var img in request.ImageElements) AddImageElement(doc, pdfDoc, img);

        doc.Close();
        await Task.CompletedTask;
        return output.ToArray();
    }

    public void CleanupDocument(string documentId) => _documents.TryRemove(documentId, out _);

    public async Task<byte[]> GetDocumentAsync(string documentId)
    {
        if (!_documents.TryGetValue(documentId, out var bytes)) throw new FileNotFoundException("Document not found", documentId);
        await Task.CompletedTask;
        return bytes;
    }

    public async Task<byte[]> GetPageAsImageAsync(string documentId, int pageNumber)
    {
        if (!_documents.TryGetValue(documentId, out var bytes)) throw new FileNotFoundException("Document not found", documentId);
        await Task.CompletedTask;
        return PlaceholderJpeg; // placeholder; client renders real page with pdf.js
    }

    private PdfEdit.Shared.Models.PdfFormField? ConvertToFormField(string name, iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            return new PdfEdit.Shared.Models.PdfFormField
            {
                Name = name,
                Type = GetFieldType(field),
                Value = field.GetValueAsString() ?? string.Empty,
                IsRequired = field.IsRequired(),
                PageNumber = GetFieldPageNumber(field),
                Bounds = GetFieldBounds(field)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Field convert failed: {Name}", name);
            return null;
        }
    }

    private RectangleModel GetFieldBounds(iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var widgets = field.GetWidgets();
            if (widgets.Count > 0)
            {
                var rect = widgets[0].GetRectangle();
                if (rect != null && rect.Size() >= 4)
                {
                    return new RectangleModel
                    {
                        X = rect.GetAsNumber(0)?.DoubleValue() ?? 0,
                        Y = rect.GetAsNumber(1)?.DoubleValue() ?? 0,
                        Width = (rect.GetAsNumber(2)?.DoubleValue() ?? 0) - (rect.GetAsNumber(0)?.DoubleValue() ?? 0),
                        Height = (rect.GetAsNumber(3)?.DoubleValue() ?? 0) - (rect.GetAsNumber(1)?.DoubleValue() ?? 0)
                    };
                }
            }
        }
        catch { }
        return new RectangleModel();
    }

    private PdfFieldType GetFieldType(iText.Forms.Fields.PdfFormField field)
    {
        var t = field.GetFormType();
        if (t.Equals(PdfName.Tx)) return PdfFieldType.Text;
        if (t.Equals(PdfName.Btn)) return PdfFieldType.Checkbox;
        if (t.Equals(PdfName.Ch)) return PdfFieldType.ComboBox;
        if (t.Equals(PdfName.Sig)) return PdfFieldType.Signature;
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
                if (page != null) return page.GetDocument().GetPageNumber(page);
            }
        }
        catch { }
        return 1;
    }

    private void AddTextElement(Document doc, PdfDocument pdfDoc, PdfTextElement t)
    {
        try
        {
            var p = new Paragraph(t.Text)
                .SetFontSize(t.FontSize)
                .SetFixedPosition(t.PageNumber, (float)t.Bounds.X, (float)t.Bounds.Y, (float)(t.Bounds.Width <= 0 ? 200 : t.Bounds.Width));
            var color = TryParseColor(t.Color);
            if (color != null) p.SetFontColor(color);
            doc.Add(p);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Add text failed {Id}", t.Id); }
    }

    private Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            hex = hex.Trim();
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length == 3) // short form rgb
            {
                hex = string.Concat(hex.Select(c => new string(c, 2)));
            }
            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                var b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return new DeviceRgb(r, g, b);
            }
        }
        catch { }
        return null;
    }

    private void AddImageElement(Document doc, PdfDocument pdfDoc, PdfImageElement img)
    {
        try
        {
            var data = ImageDataFactory.Create(Convert.FromBase64String(img.ImageData));
            var pdfImg = new iText.Layout.Element.Image(data).SetFixedPosition(img.PageNumber, (float)img.Bounds.X, (float)img.Bounds.Y);
            if (img.Bounds.Width > 0 && img.Bounds.Height > 0) pdfImg.ScaleToFit((float)img.Bounds.Width, (float)img.Bounds.Height);
            doc.Add(pdfImg);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Add image failed {Id}", img.Id); }
    }
}