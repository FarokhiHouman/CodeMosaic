using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

using Serilog;

using MessageBox = System.Windows.MessageBox; // For detailed logging

namespace CodeMosaic;
// ListViewModel for MVVM - Handles properties, commands, and listing logic with detailed logging
public class ListViewModel : INotifyPropertyChanged {
	public string FolderPath {
		get => _folderPath;
		set {
			_folderPath = value;
			OnPropertyChanged(nameof(FolderPath));
			if (!string.IsNullOrEmpty(value))
				Log.Information("User selected source folder: {FolderPath}", value); // Log folder selection
		}
	}
	public string OutputFolder {
		get => _outputFolder;
		set {
			_outputFolder = value;
			OnPropertyChanged(nameof(OutputFolder));
			if (!string.IsNullOrEmpty(value))
				Log.Information("User set output folder: {OutputFolder}", value); // Log output folder change
		}
	}
	public string OutputFileName {
		get => _outputFileName;
		set {
			_outputFileName = value;
			OnPropertyChanged(nameof(OutputFileName));
			if (!string.IsNullOrEmpty(value))
				Log.Information("User set output file name: {OutputFileName}", value); // Log file name change
		}
	}
	public string NewExtension {
		get => _newExtension;
		set {
			_newExtension = value;
			OnPropertyChanged(nameof(NewExtension));
		}
	}
	public ObservableCollection<ExtensionItem> Extensions { get; } = new() {
																			   new ExtensionItem(".cs",
																								 isSelected: true),
																			   new ExtensionItem(".csproj",
																								 isSelected: true),
																			   new ExtensionItem(".xml",
																								 isSelected: true)
																		   };
	public string StatusMessage {
		get => _statusMessage;
		set {
			_statusMessage = value;
			OnPropertyChanged(nameof(StatusMessage));
			Log.Debug("Status updated to: {StatusMessage}", value); // Log status changes for tracking
		}
	}
	public string JsonOutput {
		get => _jsonOutput;
		set {
			_jsonOutput = value;
			OnPropertyChanged(nameof(JsonOutput));
			if (!string.IsNullOrEmpty(value))
				Log.Information("JSON output generated with length: {JsonLength} characters",
								value.Length); // Log JSON generation
		}
	}
	public  ICommand BrowseFolderCommand { get; }
	public  ICommand AddExtensionCommand { get; }
	public  ICommand StartListCommand    { get; }
	private string   _folderPath;
	private string   _outputFolder   = ""; // Changed default to empty for fallback logic
	private string   _outputFileName = "FileList.json";
	private string   _newExtension;
	private string   _statusMessage = "Ready to list files.";
	private string   _jsonOutput    = "";

	public ListViewModel() {
		BrowseFolderCommand = new RelayCommand(ExecuteBrowseFolder);
		AddExtensionCommand = new RelayCommand(ExecuteAddExtension);
		StartListCommand    = new RelayCommand(ExecuteStartList, CanStartList);
		Log.Information("ListViewModel initialized - Ready for file listing operations."); // Log initialization
	}

	// Small command executions (SOLID: single responsibility)
	private void ExecuteBrowseFolder(object parameter) {
		Log.Debug("BrowseFolderCommand executed - Opening folder dialog."); // Log command start
		using FolderBrowserDialog dialog = new();
		if (dialog.ShowDialog() == DialogResult.OK) {
			FolderPath = dialog.SelectedPath;
			Log.Information("Folder selected successfully: {SelectedPath}",
							dialog.SelectedPath); // Log successful selection
		} else
			Log.Warning("Folder dialog cancelled by user."); // Log cancellation
	}

	private void ExecuteAddExtension(object parameter) {
		Log.Debug("AddExtensionCommand executed - Processing new extension: {NewExtension}",
				  NewExtension); // Log command start
		string newExt = NewExtension.Trim();
		if (!string.IsNullOrEmpty(newExt) &&
			!newExt.StartsWith("."))
			newExt = "." + newExt;
		if (!string.IsNullOrEmpty(newExt) &&
			!Extensions.Any(ext => ext.Extension.Equals(newExt, StringComparison.OrdinalIgnoreCase))) {
			Extensions.Add(new ExtensionItem(newExt, isSelected: true)); // Add as selected by default
			NewExtension = "";
			Log.Information("Extension added successfully: {NewExt}", newExt); // Log successful add
			MessageBox.Show($"Extension '{newExt}' added successfully.",
							"Success",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
		} else {
			Log.Warning("Failed to add extension - Invalid or duplicate: {NewExt}", newExt); // Log failure
			MessageBox.Show("Invalid or duplicate extension.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	private async void ExecuteStartList(object parameter) {
		Log.Information("StartListCommand executed - Starting file listing process."); // Log command start
		if (string.IsNullOrEmpty(FolderPath) ||
			!Directory.Exists(FolderPath)) {
			Log.Warning("Validation failed - Invalid folder path: {FolderPath}", FolderPath); // Log validation failure
			MessageBox.Show("Please select a valid source folder.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}

		// Get selected extensions with LINQ
		List<string> selectedExtensions = Extensions.Where(ext => ext.IsSelected).Select(ext => ext.Extension).ToList();
		Log.Information("Selected extensions for listing: {SelectedExtensions}",
						string.Join(", ", selectedExtensions)); // Log selected extensions
		if (!selectedExtensions.Any()) {
			Log.Warning("Validation failed - No extensions selected."); // Log no selection
			MessageBox.Show("Please select at least one extension.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}
		string outputUsed =
			string.IsNullOrEmpty(OutputFolder) ? FolderPath : OutputFolder; // Fallback to source if output empty
		if (string.IsNullOrEmpty(OutputFolder))
			Log.Information("Output folder not specified, falling back to source folder: {OutputUsed}",
							outputUsed); // Log fallback
		else
			Log.Information("Using specified output folder: {OutputUsed}", outputUsed); // Log specified
		string fullOutputPath = Path.Combine(outputUsed, OutputFileName);
		Log.Information("Output path prepared: {FullOutputPath}", fullOutputPath); // Log output path

		// Ensure output directory exists - Minimal directory creation
		string outputDir = Path.GetDirectoryName(fullOutputPath);
		if (!Directory.Exists(outputDir)) {
			Directory.CreateDirectory(outputDir);
			Log.Information("Created output directory: {OutputDir}", outputDir); // Log directory creation
		}
		try {
			StatusMessage = "Listing files...";
			Log.Debug("Scanning directory recursively for files..."); // Log scanning start

			// Get files with LINQ
			var files = Directory.EnumerateFiles(FolderPath, "*.*", SearchOption.AllDirectories).
								  Where(f => selectedExtensions.Any(ext => f.EndsWith(ext,
																					  StringComparison.
																						  OrdinalIgnoreCase))).
								  OrderBy(f => f).
								  Select(f => new {
													  FilePath  = f,
													  FileName  = Path.GetFileName(f),
													  Extension = Path.GetExtension(f)
												  }).
								  ToArray();
			int fileCount = files.Length;
			Log.Information("Found {FileCount} matching files in {FolderPath}",
							fileCount,
							FolderPath); // Log file count
			if (!files.Any()) {
				StatusMessage = "No matching files found.";
				Log.Warning("No files matched the selected extensions in {FolderPath}", FolderPath); // Log no files
				MessageBox.Show("No matching files found.",
								"No Files",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}

			// Serialize to JSON with options for readability
			JsonSerializerOptions options = new() { WriteIndented = true };
			string                json    = JsonSerializer.Serialize(files, options);
			Log.Debug("JSON serialization completed - Size: {JsonSize} bytes", json.Length); // Log serialization

			// Write to file
			await File.WriteAllTextAsync(fullOutputPath, json);
			Log.Information("JSON file written successfully to {FullOutputPath}", fullOutputPath); // Log write success
			JsonOutput    = json;                                                                  // Bind for display
			StatusMessage = $"Listed {fileCount} files to {fullOutputPath}.";
			MessageBox.Show($"Successfully listed {fileCount} files to {fullOutputPath}.",
							"List Completed",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
			Log.Information("File listing process completed successfully."); // Log overall success
		} catch (Exception ex) {
			StatusMessage = "Error occurred.";
			Log.Error("Error during file listing: {ErrorMessage} in {FolderPath} with output {OutputUsed}",
					  ex.Message,
					  FolderPath,
					  outputUsed); // Log detailed error with context
			MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private bool CanStartList(object parameter) =>
		!string.IsNullOrEmpty(FolderPath); // Enable button if source selected (output fallback handled in execute)

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;
}