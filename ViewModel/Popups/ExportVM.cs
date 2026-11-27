using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Extensions.Logging;
using FlashMemo.Helpers;

namespace FlashMemo.ViewModel.Popups;

public partial class ExportVM(long userId, ICardQueryService cardQService, IExportService exporter, 
                            IDialogService dialogs, Action cancel, ICollection<long> flatDeckIds, 
                            ICollection<DeckNode> deckTree, ILogger<ExportVM> logger, SafetyNet net)
                            : ImportExportVMBase(cancel, Path.Combine(
                                SpecialDirectories.MyDocuments,
                                $"{DateTime.Today:dd.MM.yyyy} FM export.txt"))
{
    #region public properties
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
    protected override void HandleFormatChanged(FileFormat value)
    {
        if (value is FileFormat.txt or FileFormat.csv && !string.IsNullOrWhiteSpace(MyPath))
            MyPath = Path.ChangeExtension(MyPath, $".{value}");

        base.HandleFormatChanged(value);
    }

    protected override ValidationResult? ValidateFormat(FileFormat value)
        => value is FileFormat.txt or FileFormat.csv
            ? base.ValidateFormat(value)
            : new ValidationResult("Choose a supported export format.");
    #endregion
    
    #region methods
    protected override async Task Confirm()
    {
        #region Snapshotting UI props and validation

        if (!Validate())
            return;

        var format = Format;
        string path = MyPath;
        string fieldSep = FieldSeparator;
        char noteSep = (format == FileFormat.txt)
            ? NoteSep.ToChar() : default;

        var columns = CsvColumns.Where(c => c.IsSelected)
            .Select(c => new CsvExportColumn(c.Name, c.PropSelector)).ToArray();

        if (format == FileFormat.csv && columns.Length == 0)
        {
            dialogs.ShowError("Select at least one column to export.");
            return;
        }
        
        if (File.Exists(path))
        {
            var answer = dialogs.ShowWarning(
                title: "Overwrite?",
                message: "File at the chosen path already exists. Do you want to overwrite it?",
                buttons: DialogButtons.YesNo);

            if (answer != DialogResult.Yes)
                return;
        }
        #endregion

        var prepared = await net.TryUnsafe(
            () => PrepareContentAsync(format, fieldSep, noteSep, columns),
            actionTried: "prepare content to export",
            retry: false);

        if (!prepared.Success || prepared.Value is null)
            return;

        var saveSuccess = await net.TryUnsafe(
            () => exporter.SaveAsync(prepared.Value, path),
            actionTried: $"export cards to {Format} format",
            retry: true);

        if (saveSuccess) Close();
    }

    private async Task<string?> PrepareContentAsync(
        FileFormat format, string fieldSep, 
        char noteSep, IEnumerable<CsvExportColumn> columns)
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
            return null;
        }

        switch (format)
        {
            case FileFormat.txt:
                return exporter.PrepareTxt(cards, fieldSep, noteSep);

            case FileFormat.csv:
                var paths = GetFullPaths(deckTree);

                if (paths.Count != flatDeckIds.Count)
                {
                    dialogs.ShowError(
                        "Something went wrong while trying to build deck hierarchical paths.");
                        
                    return null;
                }

                return exporter.PrepareCsv(cards, paths, columns);

            default: throw new NotSupportedException($"{format} not supported");
        }
    }

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