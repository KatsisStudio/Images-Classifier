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
using System.Collections.Generic;
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

        ResetAll();

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
                    AuthorChoices.AddRange(Metadatas.Select(x => x.Author).Distinct());
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

                    _currentMetadata.Id = Guid.NewGuid().ToString();
                    _currentMetadata.Format = new FileInfo(path).Extension[1..];
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
                var bmp = (Bitmap)Source;
                bmp.Save($"export/images/{_currentMetadata.Id}.{_currentMetadata.Format}");

                var w = bmp.PixelSize.Width;
                var h = bmp.PixelSize.Height;
                var ratio = w > h ? (w / 200f) : (h / 300f);

                bmp.CreateScaledBitmap(new PixelSize((int)(w / ratio), (int)(h / ratio))).Save($"export/thumbnails/{_currentMetadata.Id}.{_currentMetadata.Format}");

                ResetAll();
            }
        });

        SexesAdd = ReactiveCommand.Create(() =>
        {
            var key = SexesChoices[SexesIndex].ToLowerInvariant();
            if (_sexesContentList.ContainsKey(key)) _sexesContentList[key]++;
            else _sexesContentList.Add(key, 1);
            SexesContent = string.Join(", ", _sexesContentList.Select(x => $"{x.Key}: {x.Value}"));
            SexesIndex = 0;
        });
    }

    private void ResetAll()
    {
        Source = null;
        _currentMetadata = new()
        {
            Tags = new()
        };
    }


    private ImageData _currentMetadata;

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

    private int _sexesIndex;
    public int SexesIndex
    {
        get => _sexesIndex;
        set => this.RaiseAndSetIfChanged(ref _sexesIndex, value);
    }
    public ObservableCollection<string> SexesChoices { private set; get; } = [
        "Male", "Female", "Hermaphrodite", "Other"
    ];
    private Dictionary<string, int> _sexesContentList = new();
    private string _sexesContent = string.Empty;
    public string SexesContent
    {
        get => _sexesContent;
        set => this.RaiseAndSetIfChanged(ref _sexesContent, value);
    }
    public ICommand SexesAdd { get; }
}
