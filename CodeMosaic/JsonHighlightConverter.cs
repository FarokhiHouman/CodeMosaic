using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Documents;

using Brushes = System.Windows.Media.Brushes;


namespace CodeMosaic;
// Simple converter for JSON syntax highlighting - Returns Inline with colored Runs
public class JsonHighlightConverter : IValueConverter {
	// Small recursive method for parsing JSON to colored Inlines (SOLID)
	private Inline ParseJsonToInline(JsonElement element) {
		switch (element.ValueKind) {
			case JsonValueKind.Object:
				Span span = new();
				foreach (JsonProperty prop in element.EnumerateObject()) {
					span.Inlines.Add(new Run($"\"{prop.Name}\"") { Foreground = Brushes.Blue }); // Key blue
					span.Inlines.Add(new Run(": ") { Foreground               = Brushes.Black });
					span.Inlines.Add(ParseJsonToInline(prop.Value));
					span.Inlines.Add(new Run(",") { Foreground = Brushes.Black });
					span.Inlines.Add(new LineBreak());
				}
				return span;
			case JsonValueKind.Array:
				Span arraySpan = new();
				arraySpan.Inlines.Add(new Run("[") { Foreground = Brushes.Black });
				arraySpan.Inlines.Add(new LineBreak());
				foreach (JsonElement item in element.EnumerateArray()) {
					arraySpan.Inlines.Add(ParseJsonToInline(item));
					arraySpan.Inlines.Add(new Run(",") { Foreground = Brushes.Black });
					arraySpan.Inlines.Add(new LineBreak());
				}
				arraySpan.Inlines.Add(new Run("]") { Foreground = Brushes.Black });
				return arraySpan;
			case JsonValueKind.String:
				return new Run($"\"{element.GetString()}\"") { Foreground = Brushes.Green }; // String green
			case JsonValueKind.Number:
				return new Run(element.GetRawText()) { Foreground = Brushes.Orange }; // Number orange
			case JsonValueKind.True:
			case JsonValueKind.False:
				return new Run(element.GetRawText()) { Foreground = Brushes.Purple }; // Boolean purple
			case JsonValueKind.Null: return new Run("null") { Foreground               = Brushes.Gray }; // Null gray
			default:                 return new Run(element.GetRawText()) { Foreground = Brushes.Black };
		}
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is not string json ||
			string.IsNullOrEmpty(json))
			return new Run { Text = "No JSON to display." };
		try {
			using JsonDocument doc = JsonDocument.Parse(json);
			return ParseJsonToInline(doc.RootElement);
		} catch {
			return new Run { Text = json, Foreground = Brushes.Red }; // Error fallback
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotImplementedException();
}