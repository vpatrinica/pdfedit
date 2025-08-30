# PDF Editor (2025)

Light-weight self-hosted PDF form editor built with .NET 8 (Blazor Server + minimal ASP.NET Core API). Upload a PDF, inspect / edit form fields, inject custom text boxes & signature images, then download a flattened result.

> Rendering: Client-side page rendering uses pdf.js (loaded dynamically with CDN fallback). If pdf.js fails to load, a lightweight placeholder is shown until it becomes available.

## Project Status
This project is in light maintenance mode. No new features are planned short‑term unless a concrete personal need arises or there is explicit external demand / issues filed. Expect:
- Bug fixes / security updates (best effort)
- Minor UX polish when convenient
- No roadmap for major capabilities (collaboration, multi-file batch, advanced layout, OCR, etc.)

Feel free to open issues; they may be addressed based on impact / demand.

### Not Planned (Will Not Be Implemented Unless Significant Demand)
These items are explicitly out of scope and should be assumed "won't fix" unless there is strong, demonstrated demand:
- Multi‑PDF batch processing
- Undo / redo history stack
- Real‑time collaborative editing / multi-user cursors
- Server generated page thumbnails / previews service
- Integrated authentication / multi-tenant roles
- OCR, form auto-detection enhancements beyond current basic parsing
- Advanced layout tools (grid / alignment / snapping / rulers)
- Mobile-optimized drag positioning overhaul

## Recent Changes
- Switched to Aspire AppHost (run PdfEdit.AppHost to start everything: web + api + Redis cache)
- Fixed text color rendering (hex colors now respected in generated PDF)
- Added Target button for text elements: shows crosshair + concentric circles (radii 5,7,10) at element center for quick coordinate verification
- Added font selection for text elements (Arial, Times New Roman, Courier New mapped to core PDF fonts)

## Feature Matrix
| Area | Capability | Notes |
|------|------------|-------|
| Parse | Single PDF up to 50 MB | Stored transiently in memory (per session) |
| Form Extraction | Text, checkbox (group splitting), combo/radio (treated generic) | Checkbox groups exposed as Name#1, Name#2 ... |
| Field Editing | Change text field values; toggle checkboxes | Radio groups handled as checkbox style; flattened |
| Text Elements | Add arbitrary text (content, font size, width/height, color, font family) | PDF point coordinate space |
| Signatures | Upload PNG/JPG; position & size (height scaling) | Stored Base64 + Blob (IndexedDB) with hash de-dup |
| Coordinate Tools | Double-click canvas moves active element; Locate (+) crosshair | Crosshair auto clears after 2 s |
| Session Persistence | IndexedDB (PDF + signatures), LocalStorage (metadata) | Restores last session id |
| Processing | iText7 flatten: apply fields + text + images | Output is flattened (non-editable) |
| Rendering | pdf.js page raster (client) | Dynamic loader with multi-CDN fallback |
| Cleanup | Signature ref counting (release on delete / reset) | Prevents orphan blobs |

## Technology Stack
| Layer | Tech |
|-------|------|
| UI | Blazor Server (.NET 8), Bootstrap 5, Bootstrap Icons |
| PDF Processing | iText7 (forms + stamping) |
| Client Rendering | pdf.js (browser) |
| Imaging | SixLabors.ImageSharp (extensible) |
| Persistence | Memory (server) + IndexedDB/LocalStorage (browser) |
| Orchestration | Optional .NET Aspire AppHost |

Removed previously: Docnet.Core, System.Drawing.Common, Redis Output Cache.

## Quick Start
```bash
dotnet build
dotnet run --project PdfEdit.AppHost
```
Browse to printed web endpoint (https://localhost:7xxx).

### Run Separately
```bash
cd PdfEdit.ApiService && dotnet run
cd ../PdfEdit.Web && dotnet run
```
Adjust API base address in Program.cs if not using AppHost.

## Usage Walkthrough
1. Parse a PDF.
2. Fields panel populates; filter or edit values.
3. Add text or signature elements; adjust numeric bounds (choose font family for text as needed).
4. Use Locate to confirm coordinates or double-click page to move active element.
5. Save to download merged & flattened PDF.
6. Parse New to reset (releases signature blobs + session metadata).

## Coordinate System
- Units: PDF points (1/72 inch)
- Inputs reflect raw PDF space (not device pixels)
- pdf.js raster scale currently 1.0 (adjust in UpdatePreviewAsync if needed)

## Architecture
```
Blazor Server (UI + JS interop) --> Pdf API (iText7) --> Flattened PDF
        | IndexedDB (client)           | in-memory docs
```

## Data Lifecycle
| Stage | Browser | Server | Notes |
|-------|---------|--------|-------|
| Parse | Original PDF blob (IDB) | Raw bytes (memory dict) | Id maps across |
| Signatures | Blob (IDB, hashed) | Base64 per request | Ref counts manage cleanup |
| Metadata | LocalStorage JSON | N/A | Restored on reload |
| Output | Download only | Streamed once | Not retained |

## Extensibility Ideas
| Enhancement | Touch Points |
|-------------|--------------|
| Undo/Redo | State stack in Home.razor |
| Server Thumbnails | Add page->PNG API + cache |
| Auth | Add Identity / external provider |

## Third-Party Components & Licenses
| Component | License | Purpose |
|-----------|---------|---------|
| pdf.js | Apache 2.0 | Client PDF rendering |
| iText7 Core | AGPL / Commercial | PDF manipulation |
| iText7 BouncyCastle Adapter | AGPL | Crypto support |
| SixLabors.ImageSharp | Apache 2.0 | Image handling |
| Bootstrap | MIT | UI framework |
| Bootstrap Icons | MIT | Icons |
| .NET Runtime / ASP.NET Core | MIT | Framework |

See LICENSE plus upstream licenses. When redistributing binaries under MIT + AGPL components you must comply with AGPL (publish corresponding source or obtain commercial iText license).

## SBOM & OSS Compliance
Generate a CycloneDX SBOM (example):
```bash
dotnet tool install --global CycloneDX
cyclonedx dotnet --output sbom --json
```
Include resulting sbom.json in releases. Ensure:
- LICENSE file included
- This README (attribution section) retained
- NOTICE (optional) summarizing third-party licenses (create if distributing binaries)

## Contributing
1. Fork & branch (feat/<name>)
2. Keep PRs focused
3. Run full manual workflow before submitting

## License
MIT © 2024-2025 vpatrinica & Contributors (see LICENSE). Some dependencies under different licenses (see above).
