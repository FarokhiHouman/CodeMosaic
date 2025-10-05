using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

using MessageBox = System.Windows.MessageBox;


// For WPF MessageBox
// Only for FolderBrowserDialog

namespace CodeMosaic;
// CombineViewModel for MVVM - Handles properties, commands, and business logic
public class CombineViewModel : INotifyPropertyChanged {
	public string FolderPath {
		get => _folderPath;
		set {
			_folderPath = value;
			OnPropertyChanged(nameof(FolderPath));
		}
	}
	public string OutputFolder {
		get => _outputFolder;
		set {
			_outputFolder = value;
			OnPropertyChanged(nameof(OutputFolder));
		}
	}
	public string OutputFileName {
		get => _outputFileName;
		set {
			_outputFileName = value;
			OnPropertyChanged(nameof(OutputFileName));
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
	public string SplitMode {
		get => _splitMode;
		set {
			_splitMode = value;
			OnPropertyChanged(nameof(SplitMode));
			UpdateMaxValueUI(value);
		}
	}
	public bool IncludeMetadata {
		get => _includeMetadata;
		set {
			_includeMetadata = value;
			OnPropertyChanged(nameof(IncludeMetadata));
		}
	}
	public long MaxValue {
		get => _maxValue;
		set {
			_maxValue = value;
			OnPropertyChanged(nameof(MaxValue));
		}
	}
	public string MaxLabel {
		get => _maxLabel;
		set {
			_maxLabel = value;
			OnPropertyChanged(nameof(MaxLabel));
		}
	}
	public bool MaxVisible {
		get => _maxVisible;
		set {
			_maxVisible = value;
			OnPropertyChanged(nameof(MaxVisible));
		}
	}
	public  ICommand BrowseFolderCommand       { get; }
	public  ICommand BrowseOutputFolderCommand { get; }
	public  ICommand AddExtensionCommand       { get; }
	public  ICommand StartCombineCommand       { get; }
	private bool     _includeMetadata = true;
	private bool     _maxVisible;
	private long     _maxValue = 50000;
	private string   _folderPath;
	private string   _outputFolder;
	private string   _outputFileName = "CombinedFiles.cs";
	private string   _newExtension;
	private string   _splitMode = "None";
	private string   _maxLabel  = "Max Characters:";

	public CombineViewModel() {
		BrowseFolderCommand       = new RelayCommand(ExecuteBrowseFolder);
		BrowseOutputFolderCommand = new RelayCommand(ExecuteBrowseOutputFolder);
		AddExtensionCommand       = new RelayCommand(ExecuteAddExtension);
		StartCombineCommand       = new RelayCommand(ExecuteStartCombine, CanStartCombine);
		UpdateMaxValueUI("None"); // Initial state
	}

	// Small command executions (SOLID: single responsibility)
	private void ExecuteBrowseFolder(object parameter) {
		using FolderBrowserDialog dialog = new();
		if (dialog.ShowDialog() == DialogResult.OK)
			FolderPath = dialog.SelectedPath;
	}

	private void ExecuteBrowseOutputFolder(object parameter) {
		using FolderBrowserDialog dialog = new();
		if (dialog.ShowDialog() == DialogResult.OK)
			OutputFolder = dialog.SelectedPath;
	}

	private void ExecuteAddExtension(object parameter) {
		string newExt = NewExtension.Trim();
		if (!string.IsNullOrEmpty(newExt) &&
			!newExt.StartsWith("."))
			newExt = "." + newExt;
		if (!string.IsNullOrEmpty(newExt) &&
			!Extensions.Any(ext => ext.Extension.Equals(newExt, StringComparison.OrdinalIgnoreCase))) {
			Extensions.Add(new ExtensionItem(newExt, isSelected: true)); // Add as selected by default
			NewExtension = "";
			MessageBox.Show($"Extension '{newExt}' added successfully.",
							"Success",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
		} else
			MessageBox.Show("Invalid or duplicate extension.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
	}

	private async void ExecuteStartCombine(object parameter) {
		if (string.IsNullOrEmpty(FolderPath)   ||
			string.IsNullOrEmpty(OutputFolder) ||
			!Directory.Exists(FolderPath)) {
			MessageBox.Show("Please select valid source and output folders.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}

		// Get selected extensions with LINQ
		List<string> selectedExtensions = Extensions.Where(ext => ext.IsSelected).Select(ext => ext.Extension).ToList();
		if (!selectedExtensions.Any()) {
			MessageBox.Show("Please select at least one extension.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}
		string fullOutputPath = Path.Combine(OutputFolder, OutputFileName);
		try {
			// Get files with LINQ using selected extensions
			string[] files = Directory.EnumerateFiles(FolderPath, "*.*", SearchOption.AllDirectories).
									   Where(f => selectedExtensions.Any(ext => f.EndsWith(ext,
																						   StringComparison.
																							   OrdinalIgnoreCase))).
									   OrderBy(f => f).
									   ToArray();
			if (!files.Any()) {
				MessageBox.Show("No matching files found.",
								"No Files",
								MessageBoxButton.OK,
								MessageBoxImage.Information);
				return;
			}
			await CombineFilesAsync(files, fullOutputPath, SplitMode, MaxValue, IncludeMetadata);
			MessageBox.Show($"Successfully combined {files.Length} files into {fullOutputPath}.",
							"Combine Completed",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
		} catch (Exception ex) {
			MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private bool CanStartCombine(object parameter) =>
		!string.IsNullOrEmpty(FolderPath) && !string.IsNullOrEmpty(OutputFolder);

	// Small method for dynamic UI (SOLID)
	private void UpdateMaxValueUI(string mode) {
		MaxVisible = mode != "None";
		if (mode == "None")
			return;
		MaxLabel = mode == "By Characters" ? "Max Characters:" : "Max Size (MB):";
		// TODO: Update slider range via Binding if needed
	}

	// Async combine method (unchanged logic)
	private async Task CombineFilesAsync(string[] files,
										 string   outputPath,
										 string   splitMode,
										 long     maxValue,
										 bool     includeMetadata) {
		using StreamWriter writer    = new(outputPath, append: false);
		int                fileIndex = 0;
		foreach (string file in files) {
			string content = await File.ReadAllTextAsync(file);
			if (includeMetadata) {
				string metadata =
					$"// File {++fileIndex}: {Path.GetFileName(file)} (Size: {new FileInfo(file).Length} bytes)\n";
				await writer.WriteLineAsync(metadata);
			}
			await writer.WriteLineAsync(content);
			await writer.WriteLineAsync();
			if (splitMode                  != "None" &&
				writer.BaseStream.Position > maxValue * (splitMode == "By Size" ? 1024 * 1024 : 1))
				Debug.WriteLine($"Split triggered at file: {file}");
		}
	}

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;
}