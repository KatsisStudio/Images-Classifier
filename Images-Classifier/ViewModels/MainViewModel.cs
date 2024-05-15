using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DynamicData;
using Images_Classifier.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Images_Classifier.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        if (!Design.IsDesignMode)
        {
            if (!Directory.Exists("export")) Directory.CreateDirectory("export");
            if (!Directory.Exists("export/images")) Directory.CreateDirectory("export/images");
            if (!Directory.Exists("export/thumbnails")) Directory.CreateDirectory("export/thumbnails");
        }

        ImportMetadataCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Metadata File",
                AllowMultiple = false,
                FileTypeFilter = [
                    new FilePickerFileType("JSON file")
                    {
                        Patterns = [ "*.json" ],
                        MimeTypes = [ "application/json" ]
                    }
                ]
            });

            if (files.Any())
            {
                try
                {
                    Metadatas = JsonSerializer.Deserialize<ImageData[]>(File.ReadAllText(files.ElementAt(0).Path.AbsolutePath));

                    ParentChoices.AddRange(Metadatas.Select(x => x.Id));
                    AuthorChoices.AddRange(Metadatas.Select(x => x.Author));
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
        });

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
                try
                {
                    var path = files.ElementAt(0).Path.AbsolutePath;
                    Source = new Bitmap(path.Replace("%20", " ")); // wtf Uri
                    _extension = new FileInfo(path).Extension;
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    throw;
                }
            }
        });

        Cancel = ReactiveCommand.Create(ResetAll);
        Save = ReactiveCommand.Create(() =>
        {
            if (Source != null)
            {
                var id = Guid.NewGuid();

                var bmp = (Bitmap)Source;
                bmp.Save($"export/images/{id}{_extension}");

                var w = bmp.PixelSize.Width;
                var h = bmp.PixelSize.Height;
                var ratio = w > h ? (w / 200f) : (h / 300f);

                bmp.CreateScaledBitmap(new PixelSize((int)(w / ratio), (int)(h / ratio))).Save($"export/thumbnails/{id}{_extension}");

                ResetAll();
            }
        });
    }

    private void ResetAll()
    {
        Source = null;
    }

    private string _extension;

    private ImageData[] _metadatas;
    public ImageData[] Metadatas
    {
        get => _metadatas;
        set => this.RaiseAndSetIfChanged(ref _metadatas, value);
    }

    private IImage _source;
    public IImage Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    public ICommand ImportImageCmd { get; }
    public ICommand ImportMetadataCmd { get; }
    public ICommand Cancel { get; }
    public ICommand Save { get; }

    private string _parentText;
    public string ParentText
    {
        get => _parentText;
        set => this.RaiseAndSetIfChanged(ref _parentText, value);
    }
    public ObservableCollection<string> ParentChoices { private set; get; } = [];

    private string _authorText;
    public string AuthorText
    {
        get => _authorText;
        set => this.RaiseAndSetIfChanged(ref _authorText, value);
    }
    public ObservableCollection<string> AuthorChoices { private set; get; } = [];

    private int _ratingIndex;
    public int RatingIndex
    {
        get => _ratingIndex;
        set => this.RaiseAndSetIfChanged(ref _ratingIndex, value);
    }
    public ObservableCollection<string> RatingChoices { private set; get; } = [
        "Safe", "Questionnable", "Explicit"
    ];
}
