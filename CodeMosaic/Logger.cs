using Serilog;


namespace CodeMosaic;
// Simple Logger wrapper for Serilog - SOLID and injectable for MVVM
public class Logger {
	private readonly ILogger _logger;
	public Logger(ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	// Minimal logging methods using Serilog extensions (Information, Warning, Error) - No loops or flags
	public void LogInformation(string message) => _logger.Information(message);
	public void LogWarning(string     message) => _logger.Warning(message);
	public void LogError(string       message) => _logger.Error(message);

	// Forward Debug if needed - Keep minimal
	public void LogDebug(string message) => _logger.Debug(message);
}