# PDF Editor

A self-hosted web application for editing PDF documents. Upload PDFs, fill form fields, add text boxes, insert signature images, and download the modified document.

## Features

- **PDF Upload & Rendering**: Upload PDF files with automatic form field detection
- **Form Field Editing**: Edit existing text fields and checkboxes in PDF forms
- **Dynamic Text Addition**: Add new text boxes anywhere on PDF pages with customizable font size and color
- **Signature Support**: Upload and position signature images on PDF documents
- **Save & Download**: One-click processing to flatten all changes and download the final PDF

## Technology Stack

- **Backend**: ASP.NET Core Web API (.NET 8) with iText7 for PDF processing
- **Frontend**: Blazor WebAssembly with Bootstrap UI
- **Containerization**: Docker and Docker Compose for easy deployment

## Quick Start

### Using Docker Compose (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/vpatrinica/pdfedit.git
   cd pdfedit
   ```

2. Run with Docker Compose:
   ```bash
   docker-compose up --build
   ```

3. Open your browser and navigate to `http://localhost`

### Development Setup

#### Prerequisites
- .NET 8.0 SDK
- Docker (optional, for containerized deployment)

#### Running Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/vpatrinica/pdfedit.git
   cd pdfedit
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the API (in one terminal):
   ```bash
   cd src/PdfEdit.Api
   dotnet run
   ```

4. Run the client (in another terminal):
   ```bash
   cd src/PdfEdit.Client
   dotnet run
   ```

5. Open your browser and navigate to the client URL (typically `https://localhost:5001`)

## Usage

1. **Upload PDF**: Click "Choose File" and select a PDF document
2. **Edit Form Fields**: If the PDF contains form fields, they will appear in the sidebar for editing
3. **Add Text**: Click "Add Text" to create new text boxes that can be positioned on the PDF
4. **Add Signature**: Click "Add Signature" to upload an image file that can be positioned as a signature
5. **Customize Elements**: Adjust text size, color, and positioning using the sidebar controls
6. **Save & Download**: Click "Save & Download" to process all changes and download the final PDF

## Project Structure

```
pdfedit/
├── src/
│   ├── PdfEdit.Api/          # Backend API service
│   │   ├── Controllers/      # API controllers
│   │   ├── Services/         # PDF processing services
│   │   └── Dockerfile        # API container configuration
│   ├── PdfEdit.Client/       # Blazor WebAssembly frontend
│   │   ├── Pages/            # Razor pages
│   │   ├── Services/         # API client services
│   │   ├── wwwroot/          # Static web assets
│   │   ├── Dockerfile        # Client container configuration
│   │   └── nginx.conf        # Nginx configuration for serving
│   └── PdfEdit.Shared/       # Shared models and DTOs
├── docker-compose.yml        # Container orchestration
└── README.md
```

## API Endpoints

- `POST /api/pdf/upload` - Upload a PDF file for processing
- `POST /api/pdf/process` - Process PDF with edits and return the modified file
- `DELETE /api/pdf/{documentId}` - Clean up temporary document storage

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request
