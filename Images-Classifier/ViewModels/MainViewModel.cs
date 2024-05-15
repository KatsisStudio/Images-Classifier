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
                    PosesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Poses).Distinct());
                    ClothesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Clothes).Distinct());
                    SexesAttributesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Sexes).Distinct());
                    OthersChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Others).Distinct());
                    TextLangChoices.AddRange(Metadatas.Where(x => x != null).Select(x => x.Text.Language));
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

        PosesAdd = ReactiveCommand.Create(() =>
        {
            var key = PosesText.ToLowerInvariant();
            _posesContentList.Add(key);
            PosesContent = string.Join(", ", _posesContentList);
            PosesText = string.Empty;
        });

        ClothesAdd = ReactiveCommand.Create(() =>
        {
            var key = ClothesText.ToLowerInvariant();
            _clothesContentList.Add(key);
            ClothesContent = string.Join(", ", _clothesContentList);
            ClothesText = string.Empty;
        });

        SexesAttributesAdd = ReactiveCommand.Create(() =>
        {
            var key = SexesAttributesText.ToLowerInvariant();
            _sexesAttributesContentList.Add(key);
            SexesAttributesContent = string.Join(", ", _sexesAttributesContentList);
            SexesAttributesText = string.Empty;
        });

        OthersAdd = ReactiveCommand.Create(() =>
        {
            var key = OthersText.ToLowerInvariant();
            _othersContentList.Add(key);
            OthersContent = string.Join(", ", _othersContentList);
            OthersText = string.Empty;
        });

        TextContentAdd = ReactiveCommand.Create(() =>
        {
            var key = TextContentText.ToLowerInvariant();
            _textContentContentList.Add(key);
            TextContentContent = string.Join(", ", _textContentContentList);
            TextContentText = string.Empty;
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
        PosesContent = string.Empty;
        PosesText = string.Empty;
        _posesContentList.Clear();
        ClothesContent = string.Empty;
        ClothesText = string.Empty;
        _clothesContentList.Clear();
        SexesAttributesContent = string.Empty;
        SexesAttributesText = string.Empty;
        _sexesAttributesContentList.Clear();
        OthersContent = string.Empty;
        OthersText = string.Empty;
        _othersContentList.Clear();
        TextLangText = string.Empty;
        TextContentContent = string.Empty;
        TextContentText = string.Empty;
        _textContentContentList.Clear();
        TitleText = string.Empty;
        CommentText = string.Empty;
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

    private string _posesText;
    public string PosesText
    {
        get => _posesText;
        set => this.RaiseAndSetIfChanged(ref _posesText, value);
    }
    public ObservableCollection<string> PosesChoices { private set; get; } = [];
    private List<string> _posesContentList = [];
    private string _posesContent = string.Empty;
    public string PosesContent
    {
        get => _posesContent;
        set => this.RaiseAndSetIfChanged(ref _posesContent, value);
    }
    public ICommand PosesAdd { get; }

    private string _clothesText;
    public string ClothesText
    {
        get => _clothesText;
        set => this.RaiseAndSetIfChanged(ref _clothesText, value);
    }
    public ObservableCollection<string> ClothesChoices { private set; get; } = [];
    private List<string> _clothesContentList = [];
    private string _clothesContent = string.Empty;
    public string ClothesContent
    {
        get => _clothesContent;
        set => this.RaiseAndSetIfChanged(ref _clothesContent, value);
    }
    public ICommand ClothesAdd { get; }

    private string _sexesAttributesText;
    public string SexesAttributesText
    {
        get => _sexesAttributesText;
        set => this.RaiseAndSetIfChanged(ref _sexesAttributesText, value);
    }
    public ObservableCollection<string> SexesAttributesChoices { private set; get; } = [];
    private List<string> _sexesAttributesContentList = [];
    private string _sexesAttributesContent = string.Empty;
    public string SexesAttributesContent
    {
        get => _sexesAttributesContent;
        set => this.RaiseAndSetIfChanged(ref _sexesAttributesContent, value);
    }
    public ICommand SexesAttributesAdd { get; }

    private string _othersText;
    public string OthersText
    {
        get => _othersText;
        set => this.RaiseAndSetIfChanged(ref _othersText, value);
    }
    public ObservableCollection<string> OthersChoices { private set; get; } = [];
    private List<string> _othersContentList = [];
    private string _othersContent = string.Empty;
    public string OthersContent
    {
        get => _othersContent;
        set => this.RaiseAndSetIfChanged(ref _othersContent, value);
    }
    public ICommand OthersAdd { get; }

    private string _textLangText;
    public string TextLangText
    {
        get => _textLangText;
        set => this.RaiseAndSetIfChanged(ref _textLangText, value);
    }
    public ObservableCollection<string> TextLangChoices { private set; get; } = [];

    private string _textContentText;
    public string TextContentText
    {
        get => _textContentText;
        set => this.RaiseAndSetIfChanged(ref _textContentText, value);
    }
    private List<string> _textContentContentList = [];
    private string _textContentContent = string.Empty;
    public string TextContentContent
    {
        get => _textContentContent;
        set => this.RaiseAndSetIfChanged(ref _textContentContent, value);
    }
    public ICommand TextContentAdd { get; }

    private string _titleText = string.Empty;
    public string TitleText
    {
        get => _titleText;
        set => this.RaiseAndSetIfChanged(ref _titleText, value);
    }

    private string _commentText = string.Empty;
    public string CommentText
    {
        get => _commentText;
        set => this.RaiseAndSetIfChanged(ref _commentText, value);
    }
}
