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
using System.IO.Compression;
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
            CreateFolders();
        }

        if (File.Exists("export/info.json"))
        {
            _newMetadatas.AddRange(JsonSerializer.Deserialize<ImageData[]>(File.ReadAllText("export/info.json")));
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
                    ParodiesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Parodies).Distinct());
                    NamesChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Characters).Distinct());
                    OthersChoices.AddRange(Metadatas.SelectMany(x => x.Tags.Others).Distinct());
                    TextLangChoices.AddRange(Metadatas.Where(x => x.Text != null).Select(x => x.Text.Language).Distinct());
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
                _currentMetadata.Author = AuthorText;
                _currentMetadata.Comment = string.IsNullOrEmpty(CommentText) ? null : CommentText;
                _currentMetadata.Parent = string.IsNullOrEmpty(ParentText) ? null : ParentText;
                _currentMetadata.Rating = RatingIndex;
                _currentMetadata.Tags.Characters = _namesContentList.ToArray();
                _currentMetadata.Tags.Others = _othersContentList.ToArray();
                _currentMetadata.Tags.Parodies = _parodiesContentList.ToArray();
                _currentMetadata.Text = string.IsNullOrEmpty(TextLangText) ? null : new() { Language = TextLangText, Content = _textContentContentList.ToArray() };
                _currentMetadata.Title = string.IsNullOrEmpty(TitleText) ? null : TitleText;

                _newMetadatas.Add(_currentMetadata);

                var bmp = (Bitmap)Source;
                bmp.Save($"export/images/{_currentMetadata.Id}.{_currentMetadata.Format}");

                var w = bmp.PixelSize.Width;
                var h = bmp.PixelSize.Height;
                var ratio = w > h ? (w / 200f) : (h / 300f);

                bmp.CreateScaledBitmap(new PixelSize((int)(w / ratio), (int)(h / ratio))).Save($"export/thumbnails/{_currentMetadata.Id}.{_currentMetadata.Format}");

                File.WriteAllText("export/info.json", JsonSerializer.Serialize(_newMetadatas));

                ParentChoices.Add(_currentMetadata.Id);
                // TODO: need to do the rest later on

                ResetAll();
            }
        });

        Export = ReactiveCommand.Create(() =>
        {
            if (File.Exists("export.zip")) File.Delete("export.zip");

            ZipFile.CreateFromDirectory("export/", "export.zip");

            Process.Start("explorer.exe", "export.zip");

            Directory.Delete("export/", true);
            CreateFolders();
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

        OthersAdd = ReactiveCommand.Create(() =>
        {
            var key = OthersText.ToLowerInvariant();
            _othersContentList.Add(key);
            OthersContent = string.Join(", ", _othersContentList);
            OthersText = string.Empty;
        });

        TextContentAdd = ReactiveCommand.Create(() =>
        {
            var key = TextContentText;
            _textContentContentList.Add(key);
            TextContentContent = string.Join(", ", _textContentContentList);
            TextContentText = string.Empty;
        });
    }

    private void CreateFolders()
    {
        if (!Directory.Exists("export")) Directory.CreateDirectory("export");
        if (!Directory.Exists("export/images")) Directory.CreateDirectory("export/images");
        if (!Directory.Exists("export/thumbnails")) Directory.CreateDirectory("export/thumbnails");
    }

    private void ResetAll()
    {
        Source = null;
        _currentMetadata = new()
        {
            Tags = new()
        };
        ParodiesContent = string.Empty;
        ParodiesText = string.Empty;
        _parodiesContentList.Clear();
        NamesContent = string.Empty;
        NamesText = string.Empty;
        _namesContentList.Clear();
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

    public List<ImageData> _newMetadatas = new();

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
    public ICommand Export { get; }

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
        "Safe", "Questionable", "Explicit"
    ];

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
