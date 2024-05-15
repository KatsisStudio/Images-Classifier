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
                    RacesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Characters.Races.Select(x => x.Key).Distinct()));
                    ParodiesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Parodies).Distinct());
                    NamesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Characters.Names).Distinct());
                    RacialAttributesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Characters.RacialAttributes).Distinct());
                    AttributesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Characters.Attributes).Distinct());
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

        RacesAdd = ReactiveCommand.Create(() =>
        {
            var key = RacesContentSel.ToLowerInvariant();
            if (_racesContentList.ContainsKey(key)) _racesContentList[key]++;
            else _racesContentList.Add(key, 1);
            RacesContent = string.Join(", ", _racesContentList.Select(x => $"{x.Key}: {x.Value}"));
            RacesContentSel = string.Empty;
        });

        ParodiesAdd = ReactiveCommand.Create(() =>
        {
            var key = ParodiesText.ToLowerInvariant();
            _parodiesContentList.Add(key);
            ParodiesContent = string.Join(", ", _parodiesContentList);
            ParodiesText = string.Empty;
        });

        NamesAdd = ReactiveCommand.Create(() =>
        {
            var key = NamesText.ToLowerInvariant();
            _namesContentList.Add(key);
            NamesContent = string.Join(", ", _namesContentList);
            NamesText = string.Empty;
        });

        RacialAttributesAdd = ReactiveCommand.Create(() =>
        {
            var key = RacialAttributesText.ToLowerInvariant();
            _racialAttributesContentList.Add(key);
            RacialAttributesContent = string.Join(", ", _racialAttributesContentList);
            RacialAttributesText = string.Empty;
        });

        AttributesAdd = ReactiveCommand.Create(() =>
        {
            var key = AttributesText.ToLowerInvariant();
            _attributesContentList.Add(key);
            AttributesContent = string.Join(", ", _attributesContentList);
            AttributesText = string.Empty;
        });
    }

    private void ResetAll()
    {
        Source = null;
        _currentMetadata = new()
        {
            Tags = new()
            {
                Characters = new()
            }
        };
        SexesContent = string.Empty;
        SexesIndex = 0;
        _sexesContentList.Clear();
        RacesContent = string.Empty;
        RacesContentSel = string.Empty;
        _racesContentList.Clear();
        ParodiesContent = string.Empty;
        ParodiesText = string.Empty;
        _parodiesContentList.Clear();
        NamesContent = string.Empty;
        NamesText = string.Empty;
        _namesContentList.Clear();
        RacialAttributesContent = string.Empty;
        RacialAttributesText = string.Empty;
        _racialAttributesContentList.Clear();
        AttributesContent = string.Empty;
        AttributesText = string.Empty;
        _attributesContentList.Clear();
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
    private Dictionary<string, int> _sexesContentList = [];
    private string _sexesContent = string.Empty;
    public string SexesContent
    {
        get => _sexesContent;
        set => this.RaiseAndSetIfChanged(ref _sexesContent, value);
    }
    public ICommand SexesAdd { get; }

    private string _racesContentSel;
    public string RacesContentSel
    {
        get => _racesContentSel;
        set => this.RaiseAndSetIfChanged(ref _racesContentSel, value);
    }
    public ObservableCollection<string> RacesChoices { private set; get; } = [];
    private Dictionary<string, int> _racesContentList = [];
    private string _racesContent = string.Empty;
    public string RacesContent
    {
        get => _racesContent;
        set => this.RaiseAndSetIfChanged(ref _racesContent, value);
    }
    public ICommand RacesAdd { get; }

    private string _parodiesText;
    public string ParodiesText
    {
        get => _parodiesText;
        set => this.RaiseAndSetIfChanged(ref _parodiesText, value);
    }
    public ObservableCollection<string> ParodiesChoices { private set; get; } = [];
    private List<string> _parodiesContentList = [];
    private string _parodiesContent = string.Empty;
    public string ParodiesContent
    {
        get => _parodiesContent;
        set => this.RaiseAndSetIfChanged(ref _parodiesContent, value);
    }
    public ICommand ParodiesAdd { get; }

    private string _namesText;
    public string NamesText
    {
        get => _namesText;
        set => this.RaiseAndSetIfChanged(ref _namesText, value);
    }
    public ObservableCollection<string> NamesChoices { private set; get; } = [];
    private List<string> _namesContentList = [];
    private string _namesContent = string.Empty;
    public string NamesContent
    {
        get => _namesContent;
        set => this.RaiseAndSetIfChanged(ref _namesContent, value);
    }
    public ICommand NamesAdd { get; }

    private string _racialAttributesText;
    public string RacialAttributesText
    {
        get => _racialAttributesText;
        set => this.RaiseAndSetIfChanged(ref _racialAttributesText, value);
    }
    public ObservableCollection<string> RacialAttributesChoices { private set; get; } = [];
    private List<string> _racialAttributesContentList = [];
    private string _racialAttributesContent = string.Empty;
    public string RacialAttributesContent
    {
        get => _racialAttributesContent;
        set => this.RaiseAndSetIfChanged(ref _racialAttributesContent, value);
    }
    public ICommand RacialAttributesAdd { get; }

    private string _attributesText;
    public string AttributesText
    {
        get => _attributesText;
        set => this.RaiseAndSetIfChanged(ref _attributesText, value);
    }
    public ObservableCollection<string> AttributesChoices { private set; get; } = [];
    private List<string> _attributesContentList = [];
    private string _attributesContent = string.Empty;
    public string AttributesContent
    {
        get => _attributesContent;
        set => this.RaiseAndSetIfChanged(ref _attributesContent, value);
    }
    public ICommand AttributesAdd { get; }
}
