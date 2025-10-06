using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

using Serilog;

using MessageBox = System.Windows.MessageBox; // For detailed logging

namespace CodeMosaic;
// SplitViewModel for MVVM - Handles properties, commands, and splitting logic
public class SplitViewModel : INotifyPropertyChanged {
	public string InputFilePath {
		get => _inputFilePath;
		set {
			_inputFilePath = value;
			OnPropertyChanged(nameof(InputFilePath));
		}
	}
	public string OutputFolder {
		get => _outputFolder;
		set {
			_outputFolder = value;
			OnPropertyChanged(nameof(OutputFolder));
		}
	}
	public string BaseOutputName {
		get => _baseOutputName;
		set {
			_baseOutputName = value;
			OnPropertyChanged(nameof(BaseOutputName));
		}
	}
	public bool IsBySize {
		get => _isBySize;
		set {
			if (_isByParts)
				return; // Disable if byParts selected
			_isBySize = value;
			OnPropertyChanged(nameof(IsBySize));
			UpdateOptionsVisibility();
		}
	}
	public bool IsByChars {
		get => _isByChars;
		set {
			if (_isByParts)
				return; // Disable if byParts selected
			_isByChars = value;
			OnPropertyChanged(nameof(IsByChars));
			UpdateOptionsVisibility();
		}
	}
	public bool IsByParts {
		get => _isByParts;
		set {
			_isByParts = value;
			if (value) {
				IsBySize     = false; // Disable others
				IsByChars    = false;
				CombineModes = false;
			}
			OnPropertyChanged(nameof(IsByParts));
			UpdateOptionsVisibility();
		}
	}
	public bool CombineModes {
		get => _combineModes;
		set {
			_combineModes = value && IsBySize && IsByChars;
			OnPropertyChanged(nameof(CombineModes));
		}
	}
	public bool IsBySizeAndCharsEnabled => IsBySize && IsByChars && !IsByParts; // For CombineModes IsEnabled
	public double MaxSize {
		get => _maxSize;
		set {
			_maxSize = value;
			OnPropertyChanged(nameof(MaxSize));
		}
	}
	public long MaxChars {
		get => _maxChars;
		set {
			_maxChars = value;
			OnPropertyChanged(nameof(MaxChars));
		}
	}
	public int PartCount {
		get => _partCount;
		set {
			_partCount = value;
			OnPropertyChanged(nameof(PartCount));
		}
	}
	public string StatusMessage {
		get => _statusMessage;
		set {
			_statusMessage = value;
			OnPropertyChanged(nameof(StatusMessage));
		}
	}
	public string PartsCountMessage {
		get => _partsCountMessage;
		set {
			_partsCountMessage = value;
			OnPropertyChanged(nameof(PartsCountMessage));
		}
	}
	public  Visibility OptionsVisibility         => Visibility.Visible; // Always visible, IsEnabled controls
	public  ICommand   BrowseInputFileCommand    { get; }
	public  ICommand   BrowseOutputFolderCommand { get; }
	public  ICommand   StartSplitCommand         { get; }
	private bool       _isBySize;
	private bool       _isByChars;
	private bool       _isByParts = true; // Default to number of parts
	private bool       _combineModes;
	private double     _maxSize   = 10;    // Default 10 MB
	private int        _partCount = 2;     // Default 2 parts
	private long       _maxChars  = 50000; // Default 50k chars
	private string     _inputFilePath;
	private string     _outputFolder      = "";
	private string     _baseOutputName    = "SplitPart";
	private string     _statusMessage     = "Ready to split file.";
	private string     _partsCountMessage = "";

	public SplitViewModel() {
		BrowseInputFileCommand    = new RelayCommand(ExecuteBrowseInputFile);
		BrowseOutputFolderCommand = new RelayCommand(ExecuteBrowseOutputFolder);
		StartSplitCommand         = new RelayCommand(ExecuteStartSplit, CanStartSplit);
		Log.Information("SplitViewModel initialized - Ready for file splitting operations.");
		UpdateOptionsVisibility();
	}

	// Small command executions (SOLID: single responsibility)
	private void ExecuteBrowseInputFile(object parameter) {
		using OpenFileDialog dialog = new() { Filter = "All Files (*.*)|*.*" };
		if (dialog.ShowDialog() == DialogResult.OK) {
			InputFilePath = dialog.FileName;
			Log.Information("Input file selected for split: {InputFilePath}", dialog.FileName);
		} else
			Log.Warning("Input file dialog cancelled by user.");
	}

	private void ExecuteBrowseOutputFolder(object parameter) {
		using FolderBrowserDialog dialog = new();
		if (dialog.ShowDialog() == DialogResult.OK) {
			OutputFolder = dialog.SelectedPath;
			Log.Information("Output folder selected for split: {OutputFolder}", dialog.SelectedPath);
		} else
			Log.Warning("Output folder dialog cancelled by user.");
	}

	private async void ExecuteStartSplit(object parameter) {
		Log.Information("StartSplitCommand executed - Starting split process for {InputFilePath}", InputFilePath);
		if (string.IsNullOrEmpty(InputFilePath) ||
			!File.Exists(InputFilePath)) {
			Log.Warning("Validation failed - Invalid input file: {InputFilePath}", InputFilePath);
			MessageBox.Show("Please select a valid input file.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}
		string outputUsed =
			string.IsNullOrEmpty(OutputFolder) ?
				Path.GetDirectoryName(InputFilePath) :
				OutputFolder; // Fallback to input dir
		if (string.IsNullOrEmpty(OutputFolder))
			Log.Information("Output folder not specified, falling back to input directory: {OutputUsed}", outputUsed);
		else
			Log.Information("Using specified output folder for split: {OutputUsed}", outputUsed);
		string ext = Path.GetExtension(InputFilePath);
		Log.Information("Split mode: {SplitMode}, Max Size: {MaxSize} MB, Max Chars: {MaxChars}, Parts: {PartCount}, Combine: {CombineModes}",
						GetSplitModeDescription(),
						MaxSize,
						MaxChars,
						PartCount,
						CombineModes);

		// Ensure output directory exists
		if (!Directory.Exists(outputUsed)) {
			Directory.CreateDirectory(outputUsed);
			Log.Information("Created output directory: {OutputUsed}", outputUsed);
		}
		try {
			StatusMessage = "Splitting file...";
			int partsCreated = await SplitFileAsync(InputFilePath, outputUsed, BaseOutputName, ext);
			PartsCountMessage = $"Created {partsCreated} part(s).";
			StatusMessage     = $"Split completed successfully - {partsCreated} parts created.";
			Log.Information("Split completed successfully: {PartsCreated} parts created for {InputFilePath}",
							partsCreated);
			MessageBox.Show($"Split completed - {partsCreated} parts created.",
							"Split Completed",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
		} catch (Exception ex) {
			StatusMessage = "Split failed.";
			Log.Error("Split error for {InputFilePath}: {ErrorMessage}", InputFilePath, ex.Message);
			MessageBox.Show($"Error: {ex.Message}", "Split Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private bool CanStartSplit(object parameter) => !string.IsNullOrEmpty(InputFilePath) && File.Exists(InputFilePath);

	// Small method for mode description (SOLID)
	private string GetSplitModeDescription() =>
		IsByParts ?
			$"By {PartCount} parts" :
			IsBySize ?
				$"By size ({MaxSize} MB)" :
				IsByChars ?
					$"By chars ({MaxChars})" :
					"None";

	// Small method for UI visibility (SOLID)
	private void UpdateOptionsVisibility() {
		OnPropertyChanged(nameof(OptionsVisibility));
		OnPropertyChanged(nameof(IsBySizeAndCharsEnabled));
	}

	// Async split method (SOLID: single responsibility)
	private async Task<int> SplitFileAsync(string inputPath, string outputDir, string baseName, string ext) {
		int partsCreated = 0;
		if (IsByParts) {
			// By Parts: Even byte-based split with FileStream for precision
			long fileLength = new FileInfo(inputPath).Length;
			long partSize   = fileLength / PartCount;
			long remainder  = fileLength % PartCount;
			Log.Information("Splitting by {PartCount} parts, each approx {PartSize} bytes (remainder {Remainder})",
							PartCount,
							partSize,
							remainder);
			using FileStream inputStream    = new(inputPath, FileMode.Open, FileAccess.Read);
			int              partNum        = 1;
			byte[]           buffer         = new byte[partSize + (remainder > 0 ? 1 : 0)]; // Adjust for remainder
			long             bytesReadTotal = 0;
			while (bytesReadTotal < fileLength) {
				long currentPartSize = partSize + (partNum - 1 < remainder ? 1 : 0); // Distribute remainder
				int  bytesToRead     = (int)Math.Min(currentPartSize, buffer.Length);
				int  bytesRead       = await inputStream.ReadAsync(buffer, offset: 0, bytesToRead);
				if (bytesRead == 0)
					break;
				string partPath = Path.Combine(outputDir, $"{baseName}_part{partNum}{ext}");
				await File.WriteAllBytesAsync(partPath, buffer.Take(bytesRead).ToArray());
				partsCreated++;
				Log.Information("Created part {PartNum} ({BytesRead} bytes) for {InputPath}",
								partNum,
								bytesRead,
								inputPath);
				partNum++;
				bytesReadTotal += bytesRead;
			}
		} else {
			// By Size/Chars or Combine: Line-based split
			string             currentPartContent = "";
			long               currentSize        = 0;
			long               currentChars       = 0;
			int                partNum            = 1;
			using StreamReader reader             = new(inputPath);
			string             line;
			while ((line = await reader.ReadLineAsync()) != null) {
				string contentToAdd = line + Environment.NewLine;
				long   addedSize    = contentToAdd.Length; // Approximate byte size
				long   addedChars   = contentToAdd.Length;
				bool   shouldSplit  = false;
				if (IsBySize && currentSize + addedSize > MaxSize * 1024 * 1024)
					shouldSplit = true;
				if (IsByChars && currentChars + addedChars > MaxChars)
					shouldSplit = true;
				if (CombineModes            &&
					(IsBySize || IsByChars) &&
					shouldSplit)
					shouldSplit = true;
				if (shouldSplit) {
					if (!string.IsNullOrEmpty(currentPartContent)) {
						string partPath = Path.Combine(outputDir, $"{baseName}_part{partNum}{ext}");
						await File.WriteAllTextAsync(partPath, currentPartContent.TrimEnd());
						partsCreated++;
						Log.Information("Created line-based part {PartNum} ({CurrentSize} bytes, {CurrentChars} chars) for {InputPath}",
										partNum,
										currentSize,
										currentChars,
										inputPath);
						partNum++;
						currentPartContent = contentToAdd;
						currentSize        = addedSize;
						currentChars       = addedChars;
					}
				} else {
					currentPartContent += contentToAdd;
					currentSize        += addedSize;
					currentChars       += addedChars;
				}
			}

			// Write last part
			if (!string.IsNullOrEmpty(currentPartContent)) {
				string partPath = Path.Combine(outputDir, $"{baseName}_part{partNum}{ext}");
				await File.WriteAllTextAsync(partPath, currentPartContent.TrimEnd());
				partsCreated++;
				Log.Information("Created final line-based part {PartNum} ({CurrentSize} bytes, {CurrentChars} chars) for {InputPath}",
								partNum,
								currentSize,
								currentChars,
								inputPath);
			}
		}
		return partsCreated;
	}

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;
}