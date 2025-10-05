
# CodeMosaic
A sleek WPF desktop application for effortless C# file management – merge, split, list, and analyze CS, CSPROJ, XML, and more with MVVM precision and Serilog logging.
Features

Combine Files: Merge multiple files (e.g., CS/XAML) with metadata, split modes (size/chars), and custom extensions.
Split Files: Divide a file by size (MB), characters, number of parts, or combined criteria – creates numbered parts (e.g., Input_part1.cs).
List Files: Scan folders recursively, filter by extensions, export to colorful JSON with file paths/names/types, and display highlighted results.
MVVM Architecture: Clean separation with RelayCommands, DataBinding, and minimal code-behind for scalability.
Multi-Select Extensions: Check/uncheck file types (default: .cs, .csproj, .xml) and add custom ones dynamically.
Logging: Detailed Serilog logs in MyDocuments/CodeMosaic/Log with timestamps for every action and result.
Fallback Logic: Smart defaults (e.g., output to source folder if unspecified) and directory creation.


Combine/Split UI Example – Multi-select and mode options.
List Files JSON Highlight – Colored JSON output with 54 files example.

Installation

Clone the repo: git clone [https://github.com/yourusername/CodeMosaic.git](https://github.com/FarokhiHouman/CodeMosaic)
Open CodeMosaic.sln in Visual Studio 2022+.
Restore NuGet packages: Serilog, Serilog.Sinks.File (for logging).
Build and run – .NET 10.0-windows required.

Usage

Launch the app and select "Home" tab.
Use "Combine Files" for merging folders.
"Split Files" for dividing single files with modes (check "Combine Size & Characters" for hybrid).
"List Files" to scan/export JSON – multi-select extensions and browse folders.
Check logs in MyDocuments/CodeMosaic/Log/ for detailed actions (e.g., "Found 54 matching files").

License
This project is licensed under the MIT License - see the LICENSE file for details.
Contributing
Pull requests welcome! For major changes, open an issue first.
Built with ❤️ by Homan Farokhi – Questions? Open an issue!
