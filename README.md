# CodeMosaic

A modern WPF desktop application for streamlined C# file operations: merge, split, list, and analyze source files (CS, CSPROJ, XML, JSON, and more) with MVVM architecture, Serilog logging, and intuitive multi-select interfaces.

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview
CodeMosaic empowers developers to manage and analyze C# project files efficiently through a clean, responsive WPF UI. Built with SOLID principles, MVVM pattern, and minimal dependencies, it supports recursive folder scanning, dynamic extension filtering, and detailed logging to `MyDocuments/CodeMosaic/Log/` with timestamped files. Core features include file merging with metadata, intelligent splitting by size/chars/parts (or combined), JSON exports with syntax-highlighted previews, and advanced line counting with file-specific statistics.

## Features
- **Combine Files**: Merge folder contents (e.g., CS/XAML) with optional metadata comments, split modes (size/characters), and multi-select extensions (.cs, .csproj, .xml by default; add custom via UI).
- **Split Files**: Divide a single file into numbered parts (`Input_part1.ext`) using:
  - Max size (MB) or characters (line-based).
  - Fixed number of parts (even byte split).
  - Combined mode (size + chars for hybrid control).
  - Fallback to input directory if output unspecified; auto-creates folders.
- **List Files**: Recursively scan folders, filter by multi-select extensions, export to indented JSON (with paths, names, types), and display colored preview (keys blue, strings green, numbers orange).
- **Count Lines**: Analyze selected files/folders, count lines, and provide detailed statistics:
  - General stats (total lines, words, characters, etc.).
  - File-specific stats (e.g., classes/methods/SOLID scores for .cs via Roslyn, keys/depth for .json, tags for .xml, density for .txt).
  - Export results to CSV/JSON with DataGrid preview in UI.
- **Planned Features** (In Progress):
  - **Extract Metadata**: Pull properties, methods, and namespaces from CS files, export to JSON (TODO: Enhance Roslyn integration and UI).
  - **Settings**: Configure global preferences (e.g., default extensions, log levels, themes) with persistence (TODO: App.config and UI).

All operations include Serilog logging for actions, results, and errors, ensuring traceability without overhead.

## Installation
1. Clone the repository:
   ```
   git clone https://github.com/homanfarokhi/CodeMosaic.git
   cd CodeMosaic
   ```
2. Open `CodeMosaic.sln` in Visual Studio 2022+ (.NET 10.0-windows required).
3. Restore NuGet packages:
   ```
   dotnet restore
   ```
   (Includes Serilog for logging and Microsoft.CodeAnalysis.CSharp for Roslyn-based analysis; no other dependencies.)
4. Build and run:
   ```
   dotnet build
   dotnet run
   ```

## Usage
1. Launch the app and navigate tabs via the ribbon-style menu.
2. **Combine/Split/List/Count**: Browse folders/files, select extensions (multi-check), configure modes/options, and execute – results logged and previewed in DataGrid or JSON export.
3. **Fallbacks**: Output defaults to source if unspecified; directories auto-created.
4. **Logging**: View traces in `MyDocuments/CodeMosaic/Log/[timestamp].log` for debugging (e.g., "Found 54 files, exported to JSON" or "Counted 120 lines with 5 classes").

For advanced customization, extend ViewModels or add features via MVVM pattern.

## Architecture
- **MVVM**: Views (XAML) bound to ViewModels with RelayCommands; minimal code-behind.
- **SOLID Compliance**: Single-responsibility methods, LINQ for queries, async operations for I/O, and Roslyn for C# parsing.
- **Logging**: Serilog with rolling files; detailed traces for every user action/result, including file-specific analysis metrics.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap
- Enhance Extract Metadata and Settings (Q4 2025).
- Add unit tests (xUnit) and CI/CD (GitHub Actions).
- Support more formats (e.g., XAML preview, CSV exports).
- Optimize Count Lines for large datasets with async batch processing.

## Contributing
Contributions welcome! Fork, create a feature branch (`git checkout -b feature/amazing`), commit changes (`git commit -m 'Add amazing feature'`), and open a Pull Request. For bugs, open an issue with logs/code snippets.

Built with ❤️ using C# and WPF. Questions? Open an issue!

---
*© 2025 Homan Farokhi. Maintained with precision for developer productivity.*
