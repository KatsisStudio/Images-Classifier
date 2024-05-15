using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System.Linq;
using System.Windows.Input;

namespace Images_Classifier.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        ImportImageCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false,
                FileTypeFilter = [ FilePickerFileTypes.ImageAll ]
            });

            if (files.Any())
            {
                Source = new Bitmap(files.ElementAt(0).Path.AbsolutePath);
            }
        });
    }

    private IImage _source;
    public IImage Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    public ICommand ImportImageCmd { get; }
}
