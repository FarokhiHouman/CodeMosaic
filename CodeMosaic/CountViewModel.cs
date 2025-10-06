using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Serilog;

using MessageBox = System.Windows.MessageBox;


namespace CodeMosaic;
// StatItem model for DataGrid - Simple and bindable
public class StatItem {
	public string Metric      { get; set; }
	public string Value       { get; set; }
	public string Description { get; set; }

	public StatItem(string metric, string value, string description) {
		Metric      = metric;
		Value       = value;
		Description = description;
	}
}
// CountViewModel for MVVM - Handles properties, commands, and counting logic
public class CountViewModel : INotifyPropertyChanged {
	public string InputFilePath {
		get => _inputFilePath;
		set {
			_inputFilePath = value;
			OnPropertyChanged(nameof(InputFilePath));
		}
	}
	public string StatusMessage {
		get => _statusMessage;
		set {
			_statusMessage = value;
			OnPropertyChanged(nameof(StatusMessage));
		}
	}
	public ObservableCollection<StatItem> GeneralStatsItems {
		get => _generalStatsItems;
		set {
			_generalStatsItems = value;
			OnPropertyChanged(nameof(GeneralStatsItems));
		}
	}
	public ObservableCollection<StatItem> SpecificStatsItems {
		get => _specificStatsItems;
		set {
			_specificStatsItems = value;
			OnPropertyChanged(nameof(SpecificStatsItems));
		}
	}
	public double ProgressValue {
		get => _progressValue;
		set {
			_progressValue = value;
			OnPropertyChanged(nameof(ProgressValue));
		}
	}
	public bool IsCounting {
		get => _isCounting;
		set {
			_isCounting = value;
			OnPropertyChanged(nameof(IsCounting));
		}
	}
	public  ICommand                       BrowseInputFileCommand { get; }
	public  ICommand                       StartCountCommand      { get; }
	private bool                           _isCounting;
	private double                         _progressValue;
	private ObservableCollection<StatItem> _generalStatsItems  = new();
	private ObservableCollection<StatItem> _specificStatsItems = new();
	private string                         _inputFilePath;
	private string                         _statusMessage = "Ready to count lines.";

	public CountViewModel() {
		BrowseInputFileCommand = new RelayCommand(ExecuteBrowseInputFile);
		StartCountCommand      = new RelayCommand(ExecuteStartCount, CanStartCount);
		Log.Information("CountViewModel initialized - Ready for line counting operations.");
	}

	// Small command executions (SOLID: single responsibility)
	private void ExecuteBrowseInputFile(object parameter) {
		using OpenFileDialog dialog = new() {
												Filter =
													"Text Files (*.txt;*.cs;*.xml;*.json)|*.txt;*.cs;*.xml;*.json|All Files (*.*)|*.*"
											};
		if (dialog.ShowDialog() == DialogResult.OK) {
			InputFilePath = dialog.FileName;
			Log.Information("Input file selected for counting: {InputFilePath}", dialog.FileName);
		} else
			Log.Warning("Input file dialog cancelled by user.");
	}

	private async void ExecuteStartCount(object parameter) {
		Log.Information("StartCountCommand executed - Starting line count for {InputFilePath}", InputFilePath);
		if (string.IsNullOrEmpty(InputFilePath) ||
			!File.Exists(InputFilePath)) {
			Log.Warning("Validation failed - Invalid input file for counting: {InputFilePath}", InputFilePath);
			MessageBox.Show("Please select a valid input file.",
							"Validation Error",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}

		// Check if file is readable (e.g., not a binary/project file like .csproj)
		string extension = Path.GetExtension(InputFilePath).ToLowerInvariant();
		if (!new[] { ".txt", ".cs", ".xml", ".json" }.Contains(extension)) {
			Log.Warning("Validation failed - Unsupported file type for counting: {InputFilePath}", InputFilePath);
			MessageBox.Show("This file type is not supported for counting (e.g., .csproj). Please select a text-based file.",
							"Unsupported File Type",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
			return;
		}
		try {
			IsCounting    = true;
			ProgressValue = 0;
			StatusMessage = "Counting lines...";
			(int LineCount, int NonEmptyLineCount, int EmptyLineCount, int CommentCount, long CharCount, int WordCount,
				int UniqueWordCount, int LongestLineLength, int ShortestLineLength, double AvgLineLength, double
				FileSize, string Encoding, double ReadingTime) generalStats =
					await CountGeneralStatsAsync(InputFilePath);
			GeneralStatsItems = new ObservableCollection<StatItem> {
																	   new("Total Lines",
																		   generalStats.LineCount.ToString(),
																		   "Total number of lines in the file."),
																	   new("Non-Empty Lines",
																		   generalStats.NonEmptyLineCount.ToString(),
																		   "Lines with content (excluding blank)."),
																	   new("Empty Lines",
																		   generalStats.EmptyLineCount.ToString(),
																		   "Blank or whitespace-only lines."),
																	   new("Comment Lines",
																		   generalStats.CommentCount.ToString(),
																		   "Lines starting with // or within /* */ (code files)."),
																	   new("Total Characters",
																		   generalStats.CharCount.ToString(),
																		   "Total characters including spaces and newlines."),
																	   new("Total Words",
																		   generalStats.WordCount.ToString(),
																		   "Number of words (space/tab-separated)."),
																	   new("Unique Words",
																		   generalStats.UniqueWordCount.ToString(),
																		   "Distinct words in the file (vocabulary size)."),
																	   new("Longest Line",
																		   generalStats.LongestLineLength.ToString(),
																		   "Length of the longest line (chars)."),
																	   new("Shortest Line",
																		   generalStats.ShortestLineLength.ToString(),
																		   "Length of the shortest non-empty line (chars)."),
																	   new("Average Line Length",
																		   $"{generalStats.AvgLineLength:N2}",
																		   "Average characters per line."),
																	   new("File Size",
																		   $"{generalStats.FileSize:N2} KB",
																		   "File size in kilobytes."),
																	   new("Encoding",
																		   generalStats.Encoding,
																		   "Detected file encoding (default UTF-8)."),
																	   new("Estimated Reading Time",
																		   $"{generalStats.ReadingTime:N1} min",
																		   "Approx time to read (200 words/min).")
																   };
			SpecificStatsItems = GetFileSpecificStats(InputFilePath, generalStats); // Dedicated stats based on type
			ProgressValue      = 100;
			IsCounting         = false;
			StatusMessage      = $"Count completed for {Path.GetFileName(InputFilePath)}.";
			Log.Information("Count completed: {LineCount} lines, {CharCount} chars, {WordCount} words, {UniqueWords} unique in {InputFilePath}",
							generalStats.LineCount,
							generalStats.CharCount,
							generalStats.WordCount,
							generalStats.UniqueWordCount);
			MessageBox.Show($"Count completed:\nLines: {generalStats.LineCount}\nCharacters: {generalStats.CharCount}\nWords: {generalStats.WordCount}\nUnique Words: {generalStats.UniqueWordCount}",
							"Count Results",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
		} catch (Exception ex) {
			ProgressValue = 0;
			IsCounting    = false;
			StatusMessage = "Count failed.";
			Log.Error("Count error for {InputFilePath}: {ErrorMessage}", InputFilePath, ex.Message);
			MessageBox.Show($"Error: {ex.Message}", "Count Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private bool CanStartCount(object parameter) => !string.IsNullOrEmpty(InputFilePath) && File.Exists(InputFilePath);

	// Small method for general counting (SOLID: single responsibility)
	private async Task<(int LineCount, int NonEmptyLineCount, int EmptyLineCount, int CommentCount, long CharCount, int
		WordCount, int UniqueWordCount, int LongestLineLength, int ShortestLineLength, double AvgLineLength, double
		FileSize, string Encoding, double ReadingTime)> CountGeneralStatsAsync(string filePath) {
		int             lineCount         = 0;
		int             nonEmptyLineCount = 0;
		int             emptyLineCount    = 0;
		int             commentCount      = 0;
		long            charCount         = 0;
		int             wordCount         = 0;
		HashSet<string> uniqueWords       = new(StringComparer.OrdinalIgnoreCase);
		List<int>       lineLengths       = new();
		int             longestLine       = 0;
		int             shortestLine      = int.MaxValue;
		Regex commentRegex =
			new(@"^\s*//|/\*.*?\*/",
				RegexOptions.Multiline | RegexOptions.Compiled); // Fixed regex with non-greedy *? and escape
		using StreamReader reader = new(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		string             line;
		while ((line = await reader.ReadLineAsync()) != null) {
			lineCount++;
			int lineLength = line.Length;
			lineLengths.Add(lineLength);
			charCount += lineLength;
			if (string.IsNullOrWhiteSpace(line))
				emptyLineCount++;
			else {
				nonEmptyLineCount++;
				if (commentRegex.IsMatch(line))
					commentCount++;
				string[] words = line.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
				wordCount += words.Length;
				uniqueWords.UnionWith(words);
			}
			longestLine  = Math.Max(longestLine, lineLength);
			shortestLine = Math.Min(shortestLine, lineLength > 0 ? lineLength : shortestLine);
		}
		int    uniqueWordCount = uniqueWords.Count;
		double avgLineLength   = lineCount > 0 ? (double)charCount / lineCount : 0;
		double fileSizeKB      = new FileInfo(filePath).Length / 1024.0;
		string encoding        = reader.CurrentEncoding.WebName; // Detected encoding
		double readingTimeMin  = wordCount / 200.0;              // Approx 200 words per minute
		Log.Debug("General stats calculated: {LineCount} lines, {CharCount} chars, {WordCount} words, {UniqueWords} unique, {CommentCount} comments in {FilePath}",
				  lineCount,
				  charCount,
				  wordCount,
				  uniqueWordCount,
				  commentCount,
				  filePath);
		return (lineCount, nonEmptyLineCount, emptyLineCount, commentCount, charCount, wordCount, uniqueWordCount,
				longestLine, shortestLine == int.MaxValue ? 0 : shortestLine, avgLineLength, fileSizeKB, encoding,
				readingTimeMin);
	}

	// Small method for file-specific stats (SOLID: single responsibility)
	private ObservableCollection<StatItem> GetFileSpecificStats(string filePath,
																(int LineCount, int NonEmptyLineCount, int
																	EmptyLineCount, int CommentCount, long CharCount,
																	int WordCount, int UniqueWordCount, int
																	LongestLineLength, int ShortestLineLength, double
																	AvgLineLength, double FileSize, string Encoding,
																	double ReadingTime) generalStats) {
		string                         extension     = Path.GetExtension(filePath).ToLowerInvariant();
		ObservableCollection<StatItem> specificStats = new();
		switch (extension) {
			case ".cs": // C# file - Classes, Methods, SOLID heuristics
				try {
					(int classCount, int methodCount, SolidScores solidScores) = AnalyzeCSharpFile(filePath);
					specificStats.Add(new StatItem("Classes", classCount.ToString(),  "Number of classes/interfaces."));
					specificStats.Add(new StatItem("Methods", methodCount.ToString(), "Total methods in the file."));
					specificStats.Add(new StatItem("SRP Score",
												   $"{solidScores.SRP:N2}/10",
												   "Single Responsibility: Low method count per class (<10 ideal)."));
					specificStats.Add(new StatItem("OCP Score",
												   $"{solidScores.OCP:N2}/10",
												   "Open-Closed: Ratio of abstract/virtual methods."));
					specificStats.Add(new StatItem("LSP Score",
												   $"{solidScores.LSP:N2}/10",
												   "Liskov Substitution: Inheritance depth check."));
					specificStats.Add(new StatItem("ISP Score",
												   $"{solidScores.ISP:N2}/10",
												   "Interface Segregation: Interface implementation ratio."));
					specificStats.Add(new StatItem("DIP Score",
												   $"{solidScores.DIP:N2}/10",
												   "Dependency Inversion: Dependency count via constructor."));
					Log.Information("C# analysis completed: {ClassCount} classes, {MethodCount} methods in {FilePath}",
									classCount,
									methodCount,
									filePath);
				} catch (Exception ex) {
					Log.Error("C# analysis failed for {FilePath}: {Error}", filePath, ex.Message);
					specificStats.Add(new StatItem("C# Analysis",
												   "N/A",
												   "Error parsing C# file – fallback to general stats."));
				}
				break;
			case ".json": // JSON file - Keys, Values, Depth, Validity
				try {
					using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(filePath));
					(int keyCount, int valueCount, int depth, bool isValid) = AnalyzeJsonFile(doc.RootElement);
					specificStats.Add(new StatItem("JSON Keys",
												   keyCount.ToString(),
												   "Total unique keys in the document."));
					specificStats.Add(new StatItem("JSON Values",
												   valueCount.ToString(),
												   "Total values (strings/objects/arrays)."));
					specificStats.Add(new StatItem("Max Depth", depth.ToString(), "Deepest nested level."));
					specificStats.Add(new StatItem("Validity",  isValid ? "Valid" : "Invalid", "JSON syntax check."));
					Log.Information("JSON analysis completed: {KeyCount} keys, depth {Depth} in {FilePath}",
									keyCount,
									depth,
									filePath);
				} catch (JsonException ex) {
					Log.Error("JSON analysis failed for {FilePath}: {Error}", filePath, ex.Message);
					specificStats.Add(new StatItem("JSON Analysis",
												   "Invalid",
												   "JSON syntax error – fallback to general stats."));
				}
				break;
			case ".txt":                                         // TXT - Word density, sentence count
				double sentences = generalStats.WordCount / 5.0; // Approx 5 words per sentence
				specificStats.Add(new StatItem("Word Density",
											   $"{generalStats.WordCount / (double)generalStats.LineCount:N1} words/line",
											   "Average words per line."));
				specificStats.Add(new StatItem("Sentences", $"{sentences:N0}", "Approximate sentence count."));
				Log.Information("TXT analysis completed for {FilePath}", filePath);
				break;
			case ".xml": // XML - Tag count, attributes
				try {
					XmlDocument xmlDoc = new();
					xmlDoc.Load(filePath);
					int tagCount  = xmlDoc.SelectNodes("//*").Count;  // Fixed XPath to //* for all elements
					int attrCount = xmlDoc.SelectNodes("//@*").Count; // Fixed XPath to //@* for all attributes
					specificStats.Add(new StatItem("XML Tags",       tagCount.ToString(),  "Total element tags."));
					specificStats.Add(new StatItem("XML Attributes", attrCount.ToString(), "Total attributes."));
					Log.Information("XML analysis completed: {TagCount} tags, {AttrCount} attributes in {FilePath}",
									tagCount,
									attrCount,
									filePath);
				} catch (Exception ex) {
					Log.Error("XML analysis failed for {FilePath}: {Error}", filePath, ex.Message);
					specificStats.Add(new StatItem("XML Analysis",
												   "N/A",
												   "Error parsing XML – fallback to general stats."));
				}
				break;
			// Add more common types (e.g., .log, .html) as needed
			default:
				specificStats.Add(new StatItem("File Type",
											   extension.ToUpper(),
											   "No specific stats available – using general."));
				Log.Information("General analysis for unknown type {Extension} in {FilePath}", extension, filePath);
				break;
		}
		return specificStats;
	}

	// Small method for C# analysis with Roslyn (SOLID: single responsibility)
	private (int ClassCount, int MethodCount, SolidScores Solid) AnalyzeCSharpFile(string filePath) {
		SyntaxTree tree    = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
		SyntaxNode root    = tree.GetRoot();
		int        classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
		int        methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
		// Simple SOLID heuristics (0-10 scores)
		SolidScores solid = new() {
									  SRP = methods / Math.Max(classes, val2: 1) < 10 ? 8 : 4, // Methods per class <10
									  OCP = root.DescendantNodes().
												 OfType<MethodDeclarationSyntax>().
												 Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
															m.Modifiers.Any(SyntaxKind.VirtualKeyword)).
												 Count() /
											(double)methods >
											0.2 ?
												7 :
												3, // Abstract/virtual ratio >20%
									  LSP = root.DescendantNodes().OfType<BaseListSyntax>().Count() > 0 ?
												6 :
												2, // Has inheritance
									  ISP = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Count() > 0 ?
												7 :
												3, // Implements interfaces
									  DIP = root.DescendantNodes().
												 OfType<ConstructorDeclarationSyntax>().
												 Where(c => c.ParameterList.Parameters.Count > 0).
												 Count() /
											Math.Max(classes, val2: 1) >
											0.5 ?
												8 :
												4 // Constructor dependencies
								  };
		Log.Debug("C# analysis: {Classes} classes, {Methods} methods, SOLID scores {Solid} in {FilePath}",
				  classes,
				  methods,
				  solid,
				  filePath);
		return (classes, methods, solid);
	}

	// Small method for JSON analysis (SOLID: single responsibility)
	private (int KeyCount, int ValueCount, int Depth, bool IsValid) AnalyzeJsonFile(JsonElement root) {
		int keyCount     = 0;
		int valueCount   = 0;
		int maxDepth     = 0;
		int currentDepth = 0;
		TraverseJson(root, ref keyCount, ref valueCount, ref maxDepth, ref currentDepth);
		bool isValid = true; // Assume valid if parsed
		Log.Debug("JSON analysis: {Keys} keys, {Values} values, depth {Depth} in file", keyCount, valueCount, maxDepth);
		return (keyCount, valueCount, maxDepth, isValid);
	}

	// Recursive traverse for JSON (SOLID: helper method)
	private void TraverseJson(JsonElement element,
							  ref int     keyCount,
							  ref int     valueCount,
							  ref int     maxDepth,
							  ref int     currentDepth) {
		currentDepth++;
		maxDepth = Math.Max(maxDepth, currentDepth);
		switch (element.ValueKind) {
			case JsonValueKind.Object:
				foreach (JsonProperty prop in element.EnumerateObject()) {
					keyCount++;
					TraverseJson(prop.Value, ref keyCount, ref valueCount, ref maxDepth, ref currentDepth);
				}
				break;
			case JsonValueKind.Array:
				foreach (JsonElement item in element.EnumerateArray()) {
					TraverseJson(item, ref keyCount, ref valueCount, ref maxDepth, ref currentDepth);
				}
				break;
			case JsonValueKind.String:
			case JsonValueKind.Number:
			case JsonValueKind.True:
			case JsonValueKind.False:
			case JsonValueKind.Null:
				valueCount++;
				break;
		}
		currentDepth--;
	}

	protected virtual void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;

	// Simple SOLID scores model
	public class SolidScores {
		public double SRP { get; set; }
		public double OCP { get; set; }
		public double LSP { get; set; }
		public double ISP { get; set; }
		public double DIP { get; set; }
	}
}