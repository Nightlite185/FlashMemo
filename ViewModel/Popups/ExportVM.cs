using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.VisualBasic.FileIO;

namespace FlashMemo.ViewModel.Popups;

public enum ExportFormat { txt, csv }
public enum NoteSeparator { Tab, Enter, Space, SemiColon }

public partial class ExportVM(
    long userId, ICardQueryService cardQService,
    IExportService exporter, IDialogService dialogs, Action cancel,
    ICollection<long> flatDeckIds, ICollection<DeckNode> deckTree)
    : PopupVMBase(cancel), IViewModel
{
    #region public properties
    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ExportVM), nameof(ValidateFormat))]
    public partial ExportFormat Format { get; set; } = ExportFormat.txt;

    [ObservableProperty] [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Choose a file to export to.")]
    [CustomValidation(typeof(ExportVM), nameof(ValidateExportPath))]
    public partial string ExportPath { get; set; } = Path.Combine(SpecialDirectories.MyDocuments, $"{DateTime.Today:dd.MM.yyyy} FM export.txt");

    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ExportVM), nameof(ValidateFieldSeparator))]
    [CustomValidation(typeof(ExportVM), nameof(ValidateSeparators))]
    public partial string FieldSeparator { get; set; } = " - ";

    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ExportVM), nameof(ValidateNoteSeparator))]
    [CustomValidation(typeof(ExportVM), nameof(ValidateSeparators))]
    public partial NoteSeparator NoteSep { get; set; } = NoteSeparator.Enter;
    
    public ObservableCollection<CsvColumnOptionVM> CsvColumns { get; init; } =
    [
        new("Front content", x => x.FrontContent, true),
        new("Back content", x => x.BackContent, true),
        new("Deck", x => x.Deck, true),
        new("Due date", x => x.DueDate, true),
        new("Tags", x => x.Tags),
        new("Interval days", x => x.IntervalDays),
        new("State", x => x.State),
        new("Suspended?", x => x.IsSuspended),
        new("Buried?", x => x.IsBuried),
        new("Learning stage", x => x.LearningStage),
        new("Created date", x => x.CreatedDate),
        new("Last modified", x => x.LastModifiedDate),
        new("Last reviewed", x => x.LastReviewedDate),
    ];
    #endregion

    #region validation stuff
    internal void Validate() => ValidateAllProperties();

    partial void OnFormatChanged(ExportFormat value)
    {
        if (value is ExportFormat.txt or ExportFormat.csv && !string.IsNullOrWhiteSpace(ExportPath))
            ExportPath = Path.ChangeExtension(ExportPath, $".{value}");

        ValidateProperty(ExportPath, nameof(ExportPath));
        ValidateProperty(FieldSeparator, nameof(FieldSeparator));
        ValidateProperty(NoteSep, nameof(NoteSep));
    }

    partial void OnFieldSeparatorChanged(string value)
        => ValidateProperty(NoteSep, nameof(NoteSep));

    partial void OnNoteSepChanged(NoteSeparator value)
        => ValidateProperty(FieldSeparator, nameof(FieldSeparator));

    public static ValidationResult? ValidateFormat(ExportFormat value, ValidationContext context)
        => value is ExportFormat.txt or ExportFormat.csv
            ? ValidationResult.Success
            : new ValidationResult("Choose a supported export format.");

    public static ValidationResult? ValidateNoteSeparator(NoteSeparator value, ValidationContext context)
        => ((ExportVM)context.ObjectInstance).Format != ExportFormat.txt || Enum.IsDefined(value)
            ? ValidationResult.Success
            : new ValidationResult("Choose a note separator.");

    public static ValidationResult? ValidateFieldSeparator(string? value, ValidationContext context)
        => ((ExportVM)context.ObjectInstance).Format != ExportFormat.txt || !string.IsNullOrEmpty(value)
            ? ValidationResult.Success
            : new ValidationResult("Enter a field separator.");

    public static ValidationResult? ValidateSeparators(object? value, ValidationContext context)
    {
        var vm = (ExportVM)context.ObjectInstance;
        return vm.Format == ExportFormat.txt && Enum.IsDefined(vm.NoteSep)
            && vm.FieldSeparator == ToChar(vm.NoteSep).ToString()
            ? new ValidationResult("Field and note separators must be different.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidateExportPath(string? value, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success; // empty path already handled by [Required] attribute; no more to add.

        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return new ValidationResult("Enter a valid file path.");

        string fileName = Path.GetFileName(value);
        if (string.IsNullOrWhiteSpace(fileName) || Directory.Exists(value)
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return new ValidationResult("Choose a filename, not a folder, with valid characters.");

        var vm = (ExportVM)context.ObjectInstance;
        if (vm.Format == ExportFormat.txt
            && !string.Equals(Path.GetExtension(value), ".txt", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult("The text export filename must end in .txt.");

        return ValidationResult.Success;
    }
    #endregion
    
    #region methods
    protected override async Task Confirm()
    {
        ValidateAllProperties();

        if (HasErrors)
            return;

        var format = Format;
        var columns = CsvColumns.Where(c => c.IsSelected)
            .Select(c => new CsvExportColumn(c.Name, c.PropSelector)).ToArray();

        if (format == ExportFormat.csv && columns.Length == 0)
        {
            dialogs.ShowError("Select at least one column to export.");
            return;
        }

        string path = ExportPath;
        string fieldSep = FieldSeparator;

        char noteSep = (format == ExportFormat.txt )
            ? ToChar(NoteSep) : default;
        
        if (File.Exists(path))
        {
            var answer = dialogs.Show(
                title: "Overwrite?",
                message: "File at the chosen path already exists. Do you want to overwrite it?",
                buttons: DialogButtons.YesNo,
                icon: DialogIcons.Warning);

            if (answer != DialogResult.Yes)
                return;
        }

        string content;

        try
        {
            var cards = flatDeckIds.Count > 0
                ? await cardQService.GetCardsWhere(
                    new Filters
                    {
                        UserId = userId,
                        DeckIds = [.. flatDeckIds],
                        IncludeChildrenDecks = true
                    }, CardsOrder.Created, SortingDirection.Ascending)

                : await cardQService.GetAllFromUser(userId);

            if (cards.Count == 0)
            {
                dialogs.ShowNoCardsFoundToExport();

                Close();
                return;
            }

            switch (format)
            {
                case ExportFormat.txt:
                    content = exporter.PrepareTxt(cards, fieldSep, noteSep);
                    break;

                case ExportFormat.csv:
                    var paths = GetFullPaths(deckTree);

                    if (paths.Count != flatDeckIds.Count)
                    {
                        dialogs.ShowError(
                            "Something went wrong while trying to build deck hierarchical paths.");
                        return;
                    }
                    
                    content = exporter.PrepareCsv(cards, paths, columns);
                    break;

                default:
                    throw new NotSupportedException($"{format} not supported");
            }
        }
        catch
        {
            dialogs.ShowError("Something went wrong while preparing content to export. Try again later.");
            return;
        }

        await SaveWithRetry(content, path);
    }

    private async Task SaveWithRetry(string content, string path)
    {
        while (true)
        {
            try { await exporter.SaveAsync(content, path); }

            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                var answer = dialogs.Show(
                    title: "Failed to export content.",
                    message: $"Error: {e.Message}",
                    buttons: DialogButtons.RetryCancel,
                    icon: DialogIcons.Error);

                if (answer is DialogResult.Retry or DialogResult.TryAgain)
                    continue;

                return;
            }

            Close();
            return;
        }
    }

    private static char ToChar(NoteSeparator separator) => separator switch
    {
        NoteSeparator.Tab => '\t',
        NoteSeparator.Enter => '\n',
        NoteSeparator.SemiColon => ';',
        NoteSeparator.Space => ' ',
        _ => throw new InvalidEnumArgumentException("note separator has invalid value")
    };
    private static Dictionary<long, string> GetFullPaths(ICollection<DeckNode> deckTree)
    {
        if (deckTree.Count == 0) return [];

        var currPath = new Stack<string>();
        var deckPathMap = new Dictionary<long, string>();

        foreach(var deck in deckTree)
            backTrack(deck);

        void backTrack(DeckNode deck)
        {
            currPath.Push(deck.Name);
            
            deckPathMap.TryAdd(deck.Id, string.Join('/', currPath.Reverse()));

            foreach(var child in deck.Children)
                backTrack(child);

            currPath.Pop();
        }

        return deckPathMap;
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void ResetCsvColumns()
    {
        foreach (var col in CsvColumns)
        {
            col.IsSelected = col.IsDefault;
        }
    }

    [RelayCommand]
    private void SelectAllCsvColumns()
    {
        foreach (var col in CsvColumns)
        {
            if (!col.IsSelected)
                col.IsSelected = true;
        }
    }
    #endregion
}
