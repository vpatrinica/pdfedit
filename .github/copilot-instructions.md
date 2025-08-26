# PDF Editor - Development Instructions

**ALWAYS follow these instructions first and only fallback to additional search and context gathering if the information in these instructions is incomplete or found to be in error.**

PDF Editor is a .NET 8 web application consisting of an ASP.NET Core Web API backend and a Blazor WebAssembly frontend for editing PDF documents. Users can upload PDFs, edit form fields, add text boxes, add signature images, and download the modified documents.

## Working Effectively

### Prerequisites
- .NET 8.0 SDK (version 8.0.119 confirmed working)
- Docker (optional, for containerized deployment)

### Bootstrap and Build Process
**CRITICAL**: Build takes approximately 12 seconds. NEVER CANCEL build operations.

1. **Clean and restore dependencies:**
   ```bash
   dotnet clean
   dotnet restore
   ```
   - `dotnet clean` takes ~2 seconds
   - `dotnet restore` takes ~2 seconds (when dependencies already downloaded)

2. **Build the solution:**
   ```bash
   dotnet build
   ```
   - **Build time**: ~12 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
   - **One warning expected**: CS1998 in PdfService.cs about async method without await

### Running the Application Locally
**IMPORTANT**: There is a configuration mismatch between API and Client ports that needs to be addressed.

**Configuration Issue**: The client expects the API on `https://localhost:7099` but the API launch settings use `https://localhost:7127`.

**Solution**: Either:
1. **Option A (Recommended)**: Modify API launch settings to use port 7099:
   - Edit `src/PdfEdit.Api/Properties/launchSettings.json`
   - Change `"applicationUrl": "https://localhost:7127;http://localhost:5148"` to `"applicationUrl": "https://localhost:7099;http://localhost:5148"`

2. **Option B**: Modify client configuration:
   - Edit `src/PdfEdit.Client/Program.cs`
   - Change `"https://localhost:7099"` to `"https://localhost:7127"`

1. **First terminal - Start the API:**
   ```bash
   cd src/PdfEdit.Api
   dotnet run --launch-profile https
   ```
   - After configuration fix: API runs on `https://localhost:7099` and `http://localhost:5148`
   - Default (unfixed): API runs on `https://localhost:7127` and `http://localhost:5148`
   - Swagger UI available at the appropriate HTTPS URL + `/swagger`
   - **WARNING**: Self-signed certificate warning is expected

2. **Second terminal - Start the Client:**
   ```bash
   cd src/PdfEdit.Client
   dotnet run --launch-profile https
   ```
   - Client runs on: `https://localhost:7000` and `http://localhost:5104`
   - **WARNING**: Self-signed certificate warning is expected

3. **Access the application:**
   - Navigate to `http://localhost:5104` or `https://localhost:7000`

### Docker Deployment (Known Issue)
**WARNING**: Docker Compose build fails due to NuGet connectivity issues in containerized environments.
```bash
docker compose up --build
```
- **Does not work** due to network restrictions during NuGet package restore
- Document this limitation when Docker deployment is needed

## Validation

### Manual Testing Workflow
**ALWAYS test the complete PDF editing workflow after making changes:**

1. **Access the application** at `http://localhost:5104`
2. **Upload a PDF**: Use the file upload component to select a PDF file
3. **Verify form field detection**: Check that any PDF form fields are detected and displayed
4. **Add text elements**: Use "Add Text" button to add new text boxes
5. **Add signatures**: Use "Add Signature" button to upload and position signature images
6. **Edit form fields**: Modify any detected form field values
7. **Process and download**: Use "Save & Download" to process the PDF and download the result
8. **Verify download**: Confirm that a modified PDF file is downloaded

### Code Quality
**ALWAYS run before committing changes:**
- No formal linting tools are configured
- Build warnings should be addressed (except the known CS1998 warning)
- Manual code review is required

## Project Structure

```
pdfedit/
├── src/
│   ├── PdfEdit.Api/              # ASP.NET Core Web API backend
│   │   ├── Controllers/          # PDF upload/processing endpoints
│   │   ├── Services/             # PDF processing using iText7
│   │   ├── Properties/           # Launch settings
│   │   └── Dockerfile           # API container configuration
│   ├── PdfEdit.Client/          # Blazor WebAssembly frontend
│   │   ├── Pages/               # Razor pages (Home.razor is main)
│   │   ├── Services/            # API client services
│   │   ├── Layout/              # App layout components
│   │   ├── wwwroot/             # Static web assets (index.html, CSS)
│   │   ├── Properties/          # Launch settings
│   │   └── Dockerfile          # Client container configuration
│   └── PdfEdit.Shared/         # Shared models and DTOs
├── docker-compose.yml          # Container orchestration (non-functional)
├── PdfEdit.sln                # Visual Studio solution file
└── README.md
```

## Key Technical Details

### API Endpoints
- `POST /api/pdf/upload` - Upload PDF file, extract form fields
- `POST /api/pdf/process` - Process PDF with modifications, return modified file
- `DELETE /api/pdf/{documentId}` - Clean up temporary document storage

### Technology Stack
- **Backend**: ASP.NET Core Web API (.NET 8) with iText7 for PDF processing
- **Frontend**: Blazor WebAssembly with Bootstrap UI and JavaScript interop
- **Dependencies**: iText7, Swashbuckle (Swagger), Bootstrap, Bootstrap Icons

### Configuration Notes
- **Port Configuration Issue**: Client expects API on `https://localhost:7099` but API defaults to `https://localhost:7127`
- **CORS**: Configured for Blazor client origins (`https://localhost:5001`, `http://localhost:5000`)
- **File Limits**: 50MB maximum PDF upload size
- **SSL**: Development uses self-signed certificates (warnings expected)

## Common Issues and Solutions

1. **404 Errors on Client**: Missing wwwroot/index.html file
   - **Solution**: Ensure wwwroot/index.html and wwwroot/css/app.css exist
   
2. **API Communication Errors**: Port mismatch between API and Client
   - **Solution**: Update API launch settings to use port 7099 or update client configuration to use port 7127
   
3. **Docker Build Failures**: NuGet connectivity issues
   - **Solution**: Use local development instead of Docker in restricted environments

4. **Build Warning CS1998**: Expected warning in PdfService.cs
   - **Solution**: This warning is expected and can be ignored

## Testing Notes
- **No formal test projects** exist in the solution
- **Manual testing required** for all PDF processing functionality
- **Test files**: Use any PDF file for upload testing
- **Browser compatibility**: Modern browsers required for Blazor WebAssembly

## Common Development Tasks

### Adding New PDF Processing Features
1. **Modify models** in `PdfEdit.Shared/Class1.cs`
2. **Update API service** in `PdfEdit.Api/Services/PdfService.cs`
3. **Update controller** in `PdfEdit.Api/Controllers/PdfController.cs`
4. **Update client service** in `PdfEdit.Client/Services/PdfApiService.cs`
5. **Update UI** in `PdfEdit.Client/Pages/Home.razor`

### Debugging API Issues
1. **Check Swagger UI** at `https://localhost:7099/swagger`
2. **Check API logs** in the API terminal output
3. **Test endpoints directly** using curl or Postman

### Client-Side Debugging
1. **Browser developer tools** for JavaScript errors
2. **Check Blazor logs** in browser console
3. **Verify API connectivity** using browser network tab