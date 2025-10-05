using System.IO;
using System.Windows;

using Serilog;

using Application = System.Windows.Application;
// For WPF Application
// For logging


// For path handling

namespace CodeMosaic;
/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
	protected override void OnStartup(StartupEventArgs e) {
		// Configure Serilog with timestamped file in MyDocuments/CodeMosaic/Log
		string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
									 "CodeMosaic",
									 "Log");
		Directory.CreateDirectory(logDir); // Ensure directory exists - Minimal check
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		string logFile   = Path.Combine(logDir, $"{timestamp}.log");
		Log.Logger = new LoggerConfiguration().WriteTo.Console() // Optional console output for debugging
											  .
											   WriteTo.File(logFile,
															rollingInterval: RollingInterval.Day,
															retainedFileCountLimit: 7) // Timestamped file, keep last 7
											  .
											   CreateLogger();

		// Log app start - Minimal log
		Log.Information("CodeMosaic application started at {Timestamp}", DateTime.Now);
		base.OnStartup(e);
	}

	protected override void OnExit(ExitEventArgs e) {
		Log.Information("CodeMosaic application exited at {Timestamp}", DateTime.Now);
		Log.CloseAndFlush(); // Clean shutdown
		base.OnExit(e);
	}
}