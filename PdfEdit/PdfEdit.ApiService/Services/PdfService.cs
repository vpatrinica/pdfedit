using System.Collections.Concurrent;
using System.Linq;
using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using PdfEdit.Shared.Models;
using RectangleModel = PdfEdit.Shared.Models.Rectangle;
using SharedPdfFormField = PdfEdit.Shared.Models.PdfFormField;
using iText.Kernel.Font; // font selection
using iText.IO.Font.Constants;
using iText.IO.Font; // PdfEncodings if needed
using iText.Layout.Properties;

namespace PdfEdit.Api.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private static readonly ConcurrentDictionary<string, byte[]> _documents = new();
    private static readonly byte[] PlaceholderJpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAwT/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCfAA//2Q==");

    public PdfService(ILogger<PdfService> logger) => _logger = logger;

    public async Task<PdfUploadResponse> ExtractFormFieldsAsync(Stream pdfStream, string fileName)
    {
        var id = Guid.NewGuid().ToString();
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        _documents[id] = bytes;

        var resp = new PdfUploadResponse { Id = id, FileName = fileName };
        using var reader = new PdfReader(new MemoryStream(bytes));
        using var pdfDoc = new PdfDocument(reader);
        resp.PageCount = pdfDoc.GetNumberOfPages();
        // Populate per-page dimensions (points)
        for (int p = 1; p <= resp.PageCount; p++)
        {
            try
            {
                var page = pdfDoc.GetPage(p);
                var size = page.GetPageSize();
                resp.PageDimensions.Add(new PageDimension { PageNumber = p, Width = size.GetWidth(), Height = size.GetHeight() });
            }
            catch { resp.PageDimensions.Add(new PageDimension { PageNumber = p, Width = 0, Height = 0 }); }
        }
        var form = PdfAcroForm.GetAcroForm(pdfDoc, false);
        if (form != null)
        {
            foreach (var kv in form.GetAllFormFields())
            {
                try
                {
                    var field = kv.Value;
                    // For checkbox groups (same field name with multiple widgets) expose each widget separately: Name#1, Name#2, ...
                    if (field is PdfButtonFormField btn && IsCheckbox(btn))
                    {
                        var widgets = btn.GetWidgets();
                        if (widgets.Count > 1)
                        {
                            var currentOn = btn.GetValueAsString();
                            for (int i = 0; i < widgets.Count; i++)
                            {
                                var w = widgets[i];
                                var onState = GetOnState(btn, w);
                                var isOn = !string.IsNullOrEmpty(currentOn) && currentOn == onState && !currentOn.Equals("Off", StringComparison.OrdinalIgnoreCase);
                                resp.FormFields.Add(BuildWidgetField(kv.Key + "#" + (i + 1), PdfFieldType.Checkbox, isOn, w));
                            }
                            continue; // skip normal single add
                        }
                    }
                    var converted = ConvertField(kv.Key, field);
                    if (converted != null) resp.FormFields.Add(converted);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Extract field failed {Name}", kv.Key); }
            }
        }
        return resp;
    }

    public async Task<byte[]> ProcessPdfAsync(PdfEditRequest request)
    {
        byte[] originalPdf = !string.IsNullOrEmpty(request.OriginalPdfBase64)
            ? Convert.FromBase64String(request.OriginalPdfBase64)
            : (!string.IsNullOrEmpty(request.DocumentId) && _documents.TryGetValue(request.DocumentId, out var cached)
                ? cached
                : throw new FileNotFoundException("Document not found", request.DocumentId));

        using var input = new MemoryStream(originalPdf);
        using var output = new MemoryStream();
        using var reader = new PdfReader(input);
        using var writer = new PdfWriter(output);
        var pdfDoc = new PdfDocument(reader, writer);
        var doc = new Document(pdfDoc);

        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
        if (form != null)
        {
            var grouped = request.FormFields.GroupBy(f => BaseName(f.Name));
            foreach (var group in grouped)
            {
                try
                {
                    var baseName = group.Key;
                    var pdfField = form.GetField(baseName);
                    if (pdfField == null) continue;
                    if (pdfField is PdfButtonFormField btn && IsCheckbox(btn))
                    {
                        var widgets = btn.GetWidgets();
                        if (widgets.Count > 1)
                        {
                            bool anyOn = false; string? firstOnState = null;
                            for (int i = 0; i < widgets.Count; i++)
                            {
                                var widgetEntry = group.FirstOrDefault(g => ParseIndex(g.Name) == i + 1);
                                bool on = widgetEntry != null && IsTrue(widgetEntry.Value);
                                SetCheckboxWidgetAppearance(btn, widgets[i], on, ref firstOnState);
                                if (on) anyOn = true;
                            }
                            btn.SetValue(anyOn ? firstOnState ?? "Yes" : "Off");
                        }
                        else
                        {
                            var onVal = group.Any(g => IsTrue(g.Value));
                            SetCheckboxField(btn, onVal);
                        }
                    }
                    else
                    {
                        var first = group.First();
                        pdfField.SetValue(first.Value ?? string.Empty);
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Apply group failed {Group}", group.Key); }
            }
            form.FlattenFields();
        }

        foreach (var t in request.TextElements) AddTextElement(doc, pdfDoc, t);
        foreach (var img in request.ImageElements) AddImageElement(doc, pdfDoc, img);

        // Closing the document once; this also closes pdfDoc and flushes content.
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
        return PlaceholderJpeg;
    }

    private SharedPdfFormField? ConvertField(string name, iText.Forms.Fields.PdfFormField field)
    {
        try
        {
            var type = GetFieldType(field);
            var raw = field.GetValueAsString() ?? string.Empty;
            if (type == PdfFieldType.Checkbox)
                raw = (!string.IsNullOrEmpty(raw) && !raw.Equals("Off", StringComparison.OrdinalIgnoreCase)) ? "true" : "false";
            return new SharedPdfFormField
            {
                Name = name,
                Type = type,
                Value = raw,
                IsRequired = field.IsRequired(),
                PageNumber = GetFieldPageNumber(field),
                Bounds = GetFieldBounds(field)
            };
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Convert field failed {Name}", name); return null; }
    }

    private SharedPdfFormField BuildWidgetField(string name, PdfFieldType type, bool isOn, PdfWidgetAnnotation widget)
    {
        var rect = widget.GetRectangle();
        var bounds = new RectangleModel();
        try
        {
            if (rect != null && rect.Size() >= 4)
            {
                bounds = new RectangleModel
                {
                    X = rect.GetAsNumber(0)?.DoubleValue() ?? 0,
                    Y = rect.GetAsNumber(1)?.DoubleValue() ?? 0,
                    Width = (rect.GetAsNumber(2)?.DoubleValue() ?? 0) - (rect.GetAsNumber(0)?.DoubleValue() ?? 0),
                    Height = (rect.GetAsNumber(3)?.DoubleValue() ?? 0) - (rect.GetAsNumber(1)?.DoubleValue() ?? 0)
                };
            }
        }
        catch { }
        int pageNum = 1;
        try { var page = widget.GetPage(); if (page != null) pageNum = page.GetDocument().GetPageNumber(page); } catch { }
        return new SharedPdfFormField { Name = name, Type = type, Value = isOn ? "true" : "false", PageNumber = pageNum, Bounds = bounds };
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
        if (t.Equals(PdfName.Btn))
        {
            if (field is PdfButtonFormField btn)
            {
                try { if (btn.IsRadio()) return PdfFieldType.RadioButton; } catch { }
                return PdfFieldType.Checkbox;
            }
            return PdfFieldType.Checkbox;
        }
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

    private bool IsCheckbox(PdfButtonFormField btn)
    {
        try { if (btn.IsRadio()) return false; } catch { }
        return true;
    }

    private string GetOnState(PdfButtonFormField btn, PdfWidgetAnnotation widget)
    {
        try
        {
            var appearance = widget.GetAppearanceDictionary();
            var normal = appearance?.GetAsDictionary(PdfName.N);
            if (normal != null)
            {
                foreach (var entry in normal.KeySet())
                {
                    var v = entry.GetValue();
                    if (!v.Equals("Off", StringComparison.OrdinalIgnoreCase)) return v;
                }
            }
        }
        catch { }
        try { return btn.GetAppearanceStates().FirstOrDefault(s => !s.Equals("Off", StringComparison.OrdinalIgnoreCase)) ?? "Yes"; } catch { }
        return "Yes";
    }

    private void SetCheckboxField(PdfButtonFormField btn, bool on)
    {
        try
        {
            var states = btn.GetAppearanceStates();
            var onState = states.FirstOrDefault(s => !s.Equals("Off", StringComparison.OrdinalIgnoreCase)) ?? "Yes";
            btn.SetValue(on ? onState : "Off");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "SetCheckboxField failed"); }
    }

    private void SetCheckboxWidgetAppearance(PdfButtonFormField btn, PdfWidgetAnnotation widget, bool on, ref string? firstOnState)
    {
        try
        {
            var onState = GetOnState(btn, widget);
            if (on)
            {
                widget.SetAppearanceState(new PdfName(onState));
                if (firstOnState == null) firstOnState = onState;
            }
            else
            {
                widget.SetAppearanceState(new PdfName("Off"));
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "SetCheckboxWidgetAppearance failed"); }
    }

    private static bool IsTrue(string? v) => !string.IsNullOrEmpty(v) && !v.Equals("false", StringComparison.OrdinalIgnoreCase) && !v.Equals("Off", StringComparison.OrdinalIgnoreCase) && v != "0";
    private static string BaseName(string name) => name.Contains('#') ? name.Split('#')[0] : name;
    private static int ParseIndex(string name) { var h = name.LastIndexOf('#'); return h < 0 ? 0 : (int.TryParse(name[(h + 1)..], out var i) ? i : 0); }

    private void AddTextElement(Document doc, PdfDocument pdfDoc, PdfTextElement t)
    {
        try
        {
            var p = new Paragraph(t.Text)
                .SetFontSize(t.FontSize)
                .SetFixedPosition(t.PageNumber, (float)t.Bounds.X, (float)t.Bounds.Y, (float)(t.Bounds.Width <= 0 ? 200 : t.Bounds.Width));
            var font = ResolveFont(t.FontFamily, t.Bold, t.Italic);
            if (font != null) p.SetFont(font);
            if (t.Underline) p.SetUnderline();
            if (t.Strike) p.SetLineThrough();
            var color = TryParseColor(t.Color);
            if (color is not null) p.SetFontColor(color);
            doc.Add(p);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Add text failed {Id}", t.Id); }
    }

    private PdfFont? ResolveFont(string? family, bool bold, bool italic)
    {
        try
        {
            var key = (family ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key)) key = "Arial";
            key = key.ToLowerInvariant();
            string baseStd = key switch
            {
                "arial" => StandardFonts.HELVETICA,
                "helvetica" => StandardFonts.HELVETICA,
                "times" => StandardFonts.TIMES_ROMAN,
                "times new roman" => StandardFonts.TIMES_ROMAN,
                "courier" => StandardFonts.COURIER,
                "courier new" => StandardFonts.COURIER,
                "symbol" => StandardFonts.SYMBOL,
                "zapfdingbats" => StandardFonts.ZAPFDINGBATS,
                "georgia" => StandardFonts.TIMES_ROMAN,
                "verdana" => StandardFonts.HELVETICA,
                _ => StandardFonts.HELVETICA
            };
            // iText standard fonts treat bold/italic as separate font instances; pick closest.
            if (baseStd == StandardFonts.HELVETICA)
            {
                if (bold && italic) return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLDOBLIQUE);
                if (bold) return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                if (italic) return PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            }
            if (baseStd == StandardFonts.TIMES_ROMAN)
            {
                if (bold && italic) return PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLDITALIC);
                if (bold) return PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                if (italic) return PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }
            if (baseStd == StandardFonts.COURIER)
            {
                if (bold && italic) return PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLDOBLIQUE);
                if (bold) return PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
                if (italic) return PdfFontFactory.CreateFont(StandardFonts.COURIER_OBLIQUE);
            }
            return PdfFontFactory.CreateFont(baseStd);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResolveFont fallback to Helvetica");
            try { return PdfFontFactory.CreateFont(StandardFonts.HELVETICA); } catch { return null; }
        }
    }

    private Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            hex = hex.Trim(); if (hex.StartsWith("#")) hex = hex[1..];
            if (hex.Length == 3) hex = string.Concat(hex.Select(c => new string(c, 2)));
            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex[..2], 16);
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