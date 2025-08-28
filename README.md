# PDF Editor

A self-hosted .NET 8 PDF editing web application. Upload, edit, annotate (text + signatures) and download PDFs. Now runs via a single AppHost (Aspire) process that orchestrates the API service and Blazor Server frontend.

## Recent Changes
- Switched to Aspire AppHost (run PdfEdit.AppHost to start everything: web + api + Redis cache)
- Fixed text color rendering (hex colors now respected in generated PDF)
- Added Target button for text elements: shows crosshair + concentric circles (radii 5,7,10) at element center for quick coordinate verification

## Features
- PDF Upload & Rendering (client-side pdf.js)
- Form Field Editing (text / checkbox)
- Add / Move / Resize Text Elements (font size, color)
- Signature Image Placement
- Live Preview (server-flattened)
- Coordinate Target Visualization (one-shot overlay)
- Save & Download final flattened PDF

## Technology Stack
- Backend: ASP.NET Core Web API (.NET 8) + iText7
- Frontend: Blazor Server (interactive) using pdf.js for display
- Orchestration: .NET Aspire AppHost (web + api + Redis)
- Caching: Redis (via Aspire integration)

## Quick Start (AppHost)
1. Clone repository
2. Build solution: `dotnet build`
3. Run: `dotnet run --project PdfEdit.AppHost`
4. Open browser at the web endpoint shown (typically https://localhost:<port>)

The AppHost starts:
- apiservice (PDF processing API)
- webfrontend (Blazor UI)
- Redis (cache)

## Manual Project Run (if not using AppHost)
Run API and Web projects separately (not required if using AppHost).

## Usage
1. Upload a PDF
2. Add / edit form fields, text boxes, signatures
3. Use Target button on a text element to visualize its center coordinates (overlay auto clears)
4. Click Save to download processed PDF

## Target Overlay Details
- Drawn on current preview canvas
- Two orthogonal dashed lines (width 3)
- Concentric circles (r=5,7,10, width 3)
- Auto-clears after ~2 seconds

## Development Notes
- Text color now parsed from hex (supports #RGB and #RRGGBB)
- Preview requests debounced (350ms)
- 50 MB upload limit

## API Endpoints
- POST /api/pdf/upload
- POST /api/pdf/process
- DELETE /api/pdf/{id}

## License
MIT
