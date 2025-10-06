using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Serilog;

using Application = System.Windows.Application;


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
	public string ApplicationVersion {
		get {
			// Get version from assembly and append "Beta"
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			_applicationVersion = version != null ? $"{version.Major}.{version.Minor} Beta" : "1.0 Beta"; // Add "Beta"
			return _applicationVersion;
		}
	}
	public bool IsDarkTheme {
		get => _isDarkTheme;
		set {
			if (_isDarkTheme != value) {
				_isDarkTheme  = value;
				_isLightTheme = !value; // Ensure only one theme is active
				OnPropertyChanged(nameof(IsDarkTheme));
				OnPropertyChanged(nameof(IsLightTheme));
				ApplyTheme();   // Apply the selected theme
				SaveSettings(); // Save the new theme setting
			}
		}
	}
	public bool IsLightTheme {
		get => _isLightTheme;
		set {
			if (_isLightTheme != value) {
				_isLightTheme = value;
				_isDarkTheme  = !value; // Ensure only one theme is active
				OnPropertyChanged(nameof(IsLightTheme));
				OnPropertyChanged(nameof(IsDarkTheme));
				ApplyTheme();   // Apply the selected theme
				SaveSettings(); // Save the new theme setting
			}
		}
	}
	public        ICommand CombineCommand         { get; }
	public        ICommand SplitCommand           { get; }
	public        ICommand ListFilesCommand       { get; }
	public        ICommand CountCommand           { get; }
	public        ICommand ExtractMetadataCommand { get; }
	public        ICommand SettingsCommand        { get; }
	public        ICommand AboutCommand           { get; }
	private const string   SettingsFilePath = "settings.json"; // File to store settings

	// Logger instance from Serilog - Injected globally
	private static readonly ILogger Logger = Log.ForContext<MainViewModel>();
	private                 bool    _isDarkTheme;
	private                 bool    _isLightTheme = true; // Default to Light theme
	private                 object  _currentView;
	private                 string  _applicationVersion;

	public MainViewModel() {
		LoadSettings(); // Load saved settings on startup
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

	private void ApplyTheme() {
		if (Application.Current == null) {
			Logger.Warning("Application.Current is null, theme not applied.");
			return; // Exit if Application is not initialized
		}
		ResourceDictionary theme = new();
		if (IsDarkTheme) {
			theme.Source = new Uri("pack://application:,,,/Resources/darktheme.xaml", UriKind.RelativeOrAbsolute);
			Logger.Information("Applied Dark Theme.");
		} else {
			theme.Source = new Uri("pack://application:,,,/Resources/lighttheme.xaml", UriKind.RelativeOrAbsolute);
			Logger.Information("Applied Light Theme.");
		}
		Application.Current.Resources.MergedDictionaries.Clear();
		Application.Current.Resources.MergedDictionaries.Add(theme);
		OnPropertyChanged(nameof(IsDarkTheme));
		OnPropertyChanged(nameof(IsLightTheme));
	}

	private void LoadSettings() {
		if (File.Exists(SettingsFilePath)) {
			try {
				string        json     = File.ReadAllText(SettingsFilePath);
				SettingsData? settings = JsonSerializer.Deserialize<SettingsData>(json);
				if (settings != null) {
					_isDarkTheme  = settings.IsDarkTheme;
					_isLightTheme = !settings.IsDarkTheme; // Ensure only one is true
					ApplyTheme();                          // Apply loaded theme
					Logger.Information("Settings loaded from {FilePath}", SettingsFilePath);
				}
			} catch (Exception ex) {
				Logger.Error(ex, "Failed to load settings from {FilePath}", SettingsFilePath);
			}
		}
	}

	private void SaveSettings() {
		SettingsData settings = new() { IsDarkTheme = IsDarkTheme };
		try {
			string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(SettingsFilePath, json);
			Logger.Information("Settings saved to {FilePath}", SettingsFilePath);
		} catch (Exception ex) {
			Logger.Error(ex, "Failed to save settings to {FilePath}", SettingsFilePath);
		}
	}

	// Small methods for each command (SOLID: single responsibility) with minimal logging
	private void ExecuteCombine(object parameter) {
		Logger.Information("Navigating to CombineView.");
		CurrentView = new CombineView();
	}

	private void ExecuteSplit(object parameter) {
		Logger.Information("Navigating to SplitView.");
		CurrentView = new SplitView();
	}

	private void ExecuteListFiles(object parameter) {
		Logger.Information("Navigating to ListView.");
		CurrentView = new ListView();
	}

	private void ExecuteCount(object parameter) {
		Logger.Information("Navigating to CountView.");
		CurrentView = new CountView(); // Load CountView
	}

	private void ExecuteExtractMetadata(object parameter) {
		Logger.Information("Navigating to ExtractMetadataView.");
		CurrentView = new TextBlock { Text = "Extract Metadata Page - Pull properties, methods, and JSON export." };
	}

	private void ExecuteSettings(object parameter) {
		Logger.Information("Navigating to SettingsView.");
		CurrentView = new Settings(); // Changed to Settings UserControl
	}

	private void ExecuteAbout(object parameter) {
		Logger.Information("Navigating to About view.");
		CurrentView = new About(); // Changed to UserControl instead of MessageBox
	}

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); // Fixed with ? for null safety

	public event PropertyChangedEventHandler PropertyChanged;
}
internal class SettingsData {
	public bool IsDarkTheme { get; set; }
}