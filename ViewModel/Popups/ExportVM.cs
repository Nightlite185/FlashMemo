using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using FlashMemo.Helpers;
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
    INoteSerializer ser, Action cancel, IEnumerable<long>? deckIds = null) // if deckIds is null -> we export everything from the user.
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

        if (Format == ExportFormat.csv && !CsvColumns.Any(col => col.IsSelected))
        {
            DialogService.ShowError("Select at least one column to export.");
            return;
        }
        
        string? dir = Path.GetDirectoryName(ExportPath);

        if (dir is null)
        {
            DialogService.ShowError(
                title: "Failed to export content.",
                message: "Given path is invalid, please use another one.");
            
            return;
        }

        ICollection<(CardEntity card, Note note)> serializedPairs;

        try
        {
            var cards = (deckIds is not null)
                ? await cardQService.GetCardsWhere(
                    new Filters() 
                    { 
                        UserId = userId, 
                        DeckIds = [.. deckIds], 
                        IncludeChildrenDecks = true
                    },
                    CardsOrder.Created, 
                    SortingDirection.Ascending)

                : await cardQService.GetAllFromUser(userId);

            if (cards.Count == 0)
            {
                DialogService.ShowNoCardsFoundToExport();

                Close();
                return;
            }

            serializedPairs = ser.Serialize(cards);
        }

        catch
        {
            DialogService.ShowError(
                "Something went wrong while preparing content to export. Try again later.");

            return;
        }

        string expContent = Format switch
        {
            ExportFormat.txt => PrepareTxt([..serializedPairs.Select(p => p.note)]),
            ExportFormat.csv => await PrepareCsv(serializedPairs),

            _ => throw new NotSupportedException($"{Format} not supported")
        };
        
        await ExportToFile(expContent, dir);
    }

    private string PrepareTxt(ICollection<Note> notes)
    {
        char noteSep = ToChar(NoteSep);
        var sb = new StringBuilder();

        foreach (var note in notes)
        {
            if (note is not StandardNote sn)
                throw new NotSupportedException(
                "Only standard notes supported at the moment.");

            string entry = sn.FrontContent + FieldSeparator
                + sn.BackContent + noteSep;

            sb.Append(entry);
        }
    
        return sb.ToString();
    }
    private async Task<string> PrepareCsv(ICollection<(CardEntity card, Note note)> cards)
    {
        await using var strWriter = new StringWriter();
        await using var csv = new CsvWriter(strWriter, CultureInfo.InvariantCulture);
        csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("N/A");

        var mappedDTOs = cards.Select(
            p => new CsvExportDTO(p.card, p.note));
        
        csv.Context.RegisterClassMap(new CardCsvMap(CsvColumns));
        csv.WriteRecords(mappedDTOs);
        csv.Flush();
        
        return strWriter.ToString();
    }
    private async Task ExportToFile(string expContent, string expDirectory)
    {
        var ans = DialogResult.Retry;
        string tempWrite = Path.Combine(expDirectory,
            $"FlashMemo-{Guid.NewGuid():N}.tmp");
        
        while (ans is DialogResult.Retry or DialogResult.TryAgain)
        {
            try
            {
                await File.WriteAllTextAsync(
                    path: tempWrite,
                    contents: expContent);

                File.Move(tempWrite, ExportPath, overwrite: true);
                ans = DialogResult.None;
                Close();
            }

            catch (Exception e)
            {
                ans = DialogService.Show(
                    title: "Failed to export content.", 
                    message: $"Error: {e.Message}",
                    buttons: DialogButtons.RetryCancel,
                    icon: DialogIcons.Error);

                try { File.Delete(tempWrite); }
                catch { }
            }
        }
    }

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

    private static char ToChar(NoteSeparator sep)
    {
        return sep switch
        {
            NoteSeparator.Tab => '\t',
            NoteSeparator.Enter => '\n',
            NoteSeparator.SemiColon => ';',
            NoteSeparator.Space => ' ',

            _ => throw new InvalidEnumArgumentException("note separator has invalid value")
        };
    }
    #endregion
}
