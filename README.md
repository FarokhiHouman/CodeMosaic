
# CodeMosaic

A modern WPF desktop application for streamlined C# file operations: merge, split, list, and analyze source files (CS, CSPROJ, XML, and more) with MVVM architecture, Serilog logging, and intuitive multi-select interfaces.

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)


## Overview
CodeMosaic empowers developers to manage C# project files efficiently through a clean, responsive WPF UI. Built with SOLID principles, MVVM pattern, and minimal dependencies, it supports recursive folder scanning, dynamic extension filtering, and detailed logging to `MyDocuments/CodeMosaic/Log/` with timestamped files. Core features include file merging with metadata, intelligent splitting by size/chars/parts (or combined), and JSON exports with syntax-highlighted previews.

## Features
- **Combine Files**: Merge folder contents (e.g., CS/XAML) with optional metadata comments, split modes (size/characters), and multi-select extensions (.cs, .csproj, .xml by default; add custom via UI).
- **Split Files**: Divide a single file into numbered parts (`Input_part1.ext`) using:
  - Max size (MB) or characters (line-based).
  - Fixed number of parts (even byte split).
  - Combined mode (size + chars for hybrid control).
  - Fallback to input directory if output unspecified; auto-creates folders.
- **List Files**: Recursively scan folders, filter by multi-select extensions, export to indented JSON (with paths, names, types), and display colored preview (keys blue, strings green, numbers orange).
- **Planned Features** (In Progress):
  - **Count Lines**: Analyze and count lines across selected files/folders, with export to CSV/JSON (TODO: UI and logic implementation).
  - **Extract Metadata**: Pull properties, methods, and namespaces from CS files, export to JSON (TODO: Roslyn integration and UI).
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
   (Includes Serilog for logging; no other dependencies.)
4. Build and run:
   ```
   dotnet build
   dotnet run
   ```

## Usage
1. Launch the app and navigate tabs via the ribbon-style menu.
2. **Combine/Split/List**: Browse folders/files, select extensions (multi-check), configure modes/options, and execute – results logged and previewed.
3. **Fallbacks**: Output defaults to source if unspecified; directories auto-created.
4. **Logging**: View traces in `MyDocuments/CodeMosaic/Log/[timestamp].log` for debugging (e.g., "Found 54 files, exported to JSON").

For advanced customization, extend ViewModels or add features via MVVM pattern.

## Architecture
- **MVVM**: Views (XAML) bound to ViewModels with RelayCommands; minimal code-behind.
- **SOLID Compliance**: Single-responsibility methods, LINQ for queries, async operations for I/O.
- **Logging**: Serilog with rolling files; detailed traces for every user action/result.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap
- Implement Count Lines, Extract Metadata, and Settings (Q4 2025).
- Add unit tests (xUnit) and CI/CD (GitHub Actions).
- Support more formats (e.g., XAML preview, CSV exports).

## Contributing
Contributions welcome! Fork, create a feature branch (`git checkout -b feature/amazing`), commit changes (`git commit -m 'Add amazing feature'`), and open a Pull Request. For bugs, open an issue with logs/code snippets.

Built with ❤️ using C# and WPF. Questions? Open an issue!

---
*© 2025 Homan Farokhi. Maintained with precision for developer productivity.*
