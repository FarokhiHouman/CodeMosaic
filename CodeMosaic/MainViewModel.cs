using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Serilog;
// For WPF MessageBox
// For TextBlock
using MessageBox = System.Windows.MessageBox; // For logging

namespace CodeMosaic;
// MainViewModel for MVVM pattern - Handles navigation and commands with minimal logging
public class MainViewModel : INotifyPropertyChanged {
	public object CurrentView {
		get => _currentView;
		set {
			_currentView = value;
			OnPropertyChanged(nameof(CurrentView));
		}
	}
	public ICommand CombineCommand         { get; }
	public ICommand SplitCommand           { get; }
	public ICommand ListFilesCommand       { get; }
	public ICommand CountCommand           { get; }
	public ICommand ExtractMetadataCommand { get; }
	public ICommand SettingsCommand        { get; }
	public ICommand AboutCommand           { get; }

	// Logger instance from Serilog - Injected globally
	private static readonly ILogger Logger = Log.ForContext<MainViewModel>();
	private                 object  _currentView;

	public MainViewModel() {
		CurrentView = new TextBlock {
										Text =
											"Welcome to CodeMosaic! Select an operation from the tabs above to get started."
									}; // Initial view
		CombineCommand         = new RelayCommand(ExecuteCombine);
		SplitCommand           = new RelayCommand(ExecuteSplit);
		ListFilesCommand       = new RelayCommand(ExecuteListFiles);
		CountCommand           = new RelayCommand(ExecuteCount);
		ExtractMetadataCommand = new RelayCommand(ExecuteExtractMetadata);
		SettingsCommand        = new RelayCommand(ExecuteSettings);
		AboutCommand           = new RelayCommand(ExecuteAbout);
		Logger.Information("MainViewModel initialized - Ready for navigation.");
	}

	// Small methods for each command (SOLID: single responsibility) with minimal logging
	private void ExecuteCombine(object parameter) {
		Logger.Information("Navigating to CombineView.");
		CurrentView = new CombineView();
	}

	private void ExecuteSplit(object parameter) {
		Logger.Information("Navigating to SplitView.");
		CurrentView = new SplitView(); // Load SplitView
	}

	private void ExecuteListFiles(object parameter) {
		Logger.Information("Navigating to ListView.");
		CurrentView = new ListView();
	}

	private void ExecuteCount(object parameter) {
		Logger.Information("Navigating to CountView.");
		CurrentView = new TextBlock { Text = "Count Lines Page - Analyze and count lines in selected files." };
	}

	private void ExecuteExtractMetadata(object parameter) {
		Logger.Information("Navigating to ExtractMetadataView.");
		CurrentView = new TextBlock { Text = "Extract Metadata Page - Pull properties, methods, and JSON export." };
	}

	private void ExecuteSettings(object parameter) {
		Logger.Information("Navigating to SettingsView.");
		CurrentView = new TextBlock { Text = "Settings Page - Configure extensions, depth, and preferences." };
	}

	private void ExecuteAbout(object parameter) {
		Logger.Information("Showing About dialog.");
		MessageBox.Show("CodeMosaic v1.0\nA professional tool for C# file operations: combine, split, list, and more.\nBuilt with WPF and MVVM pattern.",
						"About CodeMosaic",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
	}

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;
}