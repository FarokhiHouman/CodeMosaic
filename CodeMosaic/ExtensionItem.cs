using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace CodeMosaic;
// Simple model for extension with selection state - SOLID and bindable
public class ExtensionItem : INotifyPropertyChanged {
	public string Extension {
		get => _extension;
		set {
			_extension = value;
			OnPropertyChanged();
		}
	}
	public bool IsSelected {
		get => _isSelected;
		set {
			_isSelected = value;
			OnPropertyChanged();
		}
	}
	private bool   _isSelected;
	private string _extension;

	public ExtensionItem(string extension, bool isSelected = true) {
		Extension  = extension;
		IsSelected = isSelected;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;
}