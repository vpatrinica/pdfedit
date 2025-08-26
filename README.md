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
- **Containerization**: Docker and Podman support for easy deployment

## Quick Start

### Using Podman (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/vpatrinica/pdfedit.git
   cd pdfedit
   ```

2. Run with Podman:
   ```bash
   # Linux/macOS:
   ./scripts/start-podman.sh
   
   # Windows:
   scripts\start-podman.bat
   ```

3. Open your browser and navigate to `http://localhost`

### Using Docker Compose

1. Clone the repository:
   ```bash
   git clone https://github.com/vpatrinica/pdfedit.git
   cd pdfedit
   ```

2. Run with Docker Compose:
   ```bash
   # Linux/macOS:
   ./scripts/start-docker.sh
   
   # Windows:
   scripts\start-docker.bat
   
   # OR manually:
   docker compose up --build
   ```

3. Open your browser and navigate to `http://localhost`

### Development Setup

#### Prerequisites
- .NET 8.0 SDK
- Docker or Podman (optional, for containerized deployment)

#### Running Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/vpatrinica/pdfedit.git
   cd pdfedit
   ```

2. Restore dependencies and build the solution:
   ```bash
   dotnet build
   ```

3. **Fix Port Mismatch (Important for local dev):**
   Edit `src/PdfEdit.Api/Properties/launchSettings.json` and change the `applicationUrl` for the `https` profile to use port `7099` instead of `7127`.

4. Run the API (in one terminal):
   ```bash
   cd src/PdfEdit.Api
   dotnet run --launch-profile https
   ```

5. Run the client (in another terminal):
   ```bash
   cd src/PdfEdit.Client
   dotnet run --launch-profile https
   ```

6. Open your browser and navigate to the client URL (typically `https://localhost:7000`)

#### Windows Development

Windows developers can use the provided batch files for container deployment:

**Using Podman (Recommended for Windows):**
```cmd
REM Start the application
scripts\start-podman.bat

REM Stop the application  
scripts\stop-podman.bat

REM Validate setup
scripts\validate.bat
```

**Using Docker:**
```cmd
REM Start the application
scripts\start-docker.bat

REM Stop the application
scripts\stop-docker.bat
```

**Prerequisites for Windows:**
- .NET 8.0 SDK
- Podman Desktop or Docker Desktop
- Windows 10/11 (batch files require Command Prompt or PowerShell)

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
│   ├── PdfEdit.Api/          # Backend API and Frontend Host
│   │   ├── Controllers/      # API controllers
│   │   ├── Services/         # PDF processing services
│   │   └── Dockerfile        # Combined Docker configuration
│   ├── PdfEdit.Client/       # Blazor WebAssembly frontend
│   │   ├── Pages/            # Razor pages
│   │   └── wwwroot/          # Static web assets
│   └── PdfEdit.Shared/       # Shared models and DTOs
├── scripts/
│   ├── start-podman.sh       # Start with Podman (Linux/macOS)
│   ├── start-podman.bat      # Start with Podman (Windows)
│   ├── stop-podman.sh        # Stop Podman containers (Linux/macOS)
│   ├── stop-podman.bat       # Stop Podman containers (Windows)
│   ├── start-docker.sh       # Start with Docker Compose (Linux/macOS)
│   ├── start-docker.bat      # Start with Docker Compose (Windows)
│   ├── stop-docker.sh        # Stop Docker containers (Linux/macOS)
│   ├── stop-docker.bat       # Stop Docker containers (Windows)
│   ├── validate.sh           # Validate container configurations (Linux/macOS)
│   └── validate.bat          # Validate container configurations (Windows)
├── docker-compose.yml        # Docker Compose configuration
├── podman-compose.yml        # Podman Compose configuration
└── README.md
```

## Container Deployment Options

The application is deployed as a single container where the ASP.NET Core backend serves the Blazor WebAssembly frontend.

### Podman (Recommended)

Podman is a daemonless container engine that's more secure and doesn't require root privileges. 

**Why Podman?**
- Rootless containers by default (better security)
- No daemon required (lower resource usage)
- Docker-compatible API
- Better integration with systemd
- Supports Kubernetes YAML files

**Quick start:**
```bash
./scripts/start-podman.sh
```

**Stop the application:**
```bash
./scripts/stop-podman.sh
```

**Manual commands:**
```bash
# Using podman-compose (if installed)
podman-compose -f podman-compose.yml up --build

# Using podman compose (built-in)
podman compose -f podman-compose.yml up --build
```

### Docker Compose

Traditional Docker Compose is also supported for teams already using Docker.

**Quick start:**
```bash
./scripts/start-docker.sh
```

**Stop the application:**
```bash
./scripts/stop-docker.sh
```

**Manual commands:**
```bash
# Using docker-compose
docker-compose up --build

# Using docker compose (newer)
docker compose up --build
```

## Troubleshooting

### Container Build Issues

**Issue**: `dotnet restore` fails during `docker build` in restricted environments.

**Solution**: This is a known limitation in networks with restricted internet access. Ensure your Docker/Podman environment can reach `nuget.org`. If not, you must use the local development setup.

### Port Conflicts

**Issue**: Port 80 is already in use on your machine.

**Solution**: Modify the port mappings in `docker-compose.yml` or `podman-compose.yml`:
```yaml
ports:
  - "8080:80"    # Change host port from 80 to 8080
```
Then access the application at `http://localhost:8080`.

### Container Validation

Use the validation script to check your setup:
```bash
# Linux/macOS:
./scripts/validate.sh

# Windows:
scripts\validate.bat
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
