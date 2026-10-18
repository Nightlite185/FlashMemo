using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using Microsoft.VisualBasic.FileIO;

namespace FlashMemo.ViewModel.Popups;

public enum ExportFormat { txt } // csv, FMExp
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
    [Required(AllowEmptyStrings = true, ErrorMessage = "Enter a field separator.")]
    [MinLength(1, ErrorMessage = "Enter a field separator.")]
    [CustomValidation(typeof(ExportVM), nameof(ValidateSeparators))]
    public partial string FieldSeparator { get; set; } = " - ";

    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ExportVM), nameof(ValidateNoteSeparator))]
    [CustomValidation(typeof(ExportVM), nameof(ValidateSeparators))]
    public partial NoteSeparator NoteSep { get; set; } = NoteSeparator.Enter;
    #endregion

    #region validation stuff
    internal void Validate() => ValidateAllProperties();

    partial void OnFormatChanged(ExportFormat value)
        => ValidateProperty(ExportPath, nameof(ExportPath));

    partial void OnFieldSeparatorChanged(string value)
        => ValidateProperty(NoteSep, nameof(NoteSep));

    partial void OnNoteSepChanged(NoteSeparator value)
        => ValidateProperty(FieldSeparator, nameof(FieldSeparator));

    public static ValidationResult? ValidateFormat(ExportFormat value, ValidationContext context)
        => value == ExportFormat.txt
            ? ValidationResult.Success
            : new ValidationResult("Choose a supported export format.");

    public static ValidationResult? ValidateNoteSeparator(NoteSeparator value, ValidationContext context)
        => Enum.IsDefined(value)
            ? ValidationResult.Success
            : new ValidationResult("Choose a note separator.");

    public static ValidationResult? ValidateSeparators(object? value, ValidationContext context)
    {
        var vm = (ExportVM)context.ObjectInstance;
        return Enum.IsDefined(vm.NoteSep) && vm.FieldSeparator == ToChar(vm.NoteSep).ToString()
            ? new ValidationResult("Field and note separators must be different.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidateExportPath(string? value, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success; // Required supplies the error.

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
    protected override async Task Confirm()
    {
        ValidateAllProperties();
        if (HasErrors)
            return;

        // TODO: main window should have bigger min size or *make export popup scrollable* OR BOTH
        
        if (Format is not ExportFormat.txt) // TODO: delete this line when implemented more formats
            return;

        char noteSep = ToChar(NoteSep);
        var sb = new StringBuilder();
        string? dir = Path.GetDirectoryName(ExportPath);

        if (dir is null)
        {
            DialogService.Show(
            title: "Failed to export content.",
            message: "Given path is invalid, please use another one.",
            buttons: DialogButtons.OK,
            icon: DialogIcons.Error);
            
            return;
        }

        string fileName = Path.GetFileName(ExportPath);
        string tempWrite = Path.Combine(dir,
            $"FlashMemo-{Guid.NewGuid():N}.tmp");

        ICollection<Note> notes;

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

            notes = ser.Serialize(
                cards.Select(c => c.Note));
        }

        catch
        {
            DialogService.ShowError(
                "Something went wrong while preparing content to export. Try again later.");

            return;
        }

        foreach (var note in notes)
        {
            if (note is not StandardNote sn)
                throw new NotSupportedException(
                "Only standard notes supported at the moment.");

            string entry = sn.FrontContent + FieldSeparator
                + sn.BackContent + noteSep;

            sb.Append(entry);
        }

        string expContent = sb.ToString();
        var ans = DialogResult.Retry;

        while (ans is DialogResult.Retry or DialogResult.TryAgain)
        {
            try
            {
                await File.WriteAllTextAsync(
                    path: tempWrite,
                    contents: expContent);

                File.Move(tempWrite, fileName, overwrite: true);
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
}
