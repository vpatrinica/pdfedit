namespace PdfEdit.Shared.Models;

public class PdfUploadResponse
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public List<PdfFormField> FormFields { get; set; } = new();
    public List<PageDimension> PageDimensions { get; set; } = new(); // new: per-page size in points
}

public class PageDimension
{
    public int PageNumber { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class PdfFormField
{
    public string Name { get; set; } = string.Empty;
    public PdfFieldType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int PageNumber { get; set; }
    public Rectangle Bounds { get; set; } = new();
}

public class PdfEditRequest
{
    public string DocumentId { get; set; } = string.Empty; // optional now
    public string? OriginalPdfBase64 { get; set; } // new for stateless processing
    public List<PdfFormField> FormFields { get; set; } = new();
    public List<PdfTextElement> TextElements { get; set; } = new();
    public List<PdfImageElement> ImageElements { get; set; } = new();
}

public class PdfTextElement
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public Rectangle Bounds { get; set; } = new();
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public string Color { get; set; } = "#000000";
}

public class PdfImageElement
{
    public string Id { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty; // Base64 encoded
    public int PageNumber { get; set; }
    public Rectangle Bounds { get; set; } = new();
}

public class Rectangle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public enum PdfFieldType
{
    Text,
    Checkbox,
    RadioButton,
    ComboBox,
    ListBox,
    Signature
}
