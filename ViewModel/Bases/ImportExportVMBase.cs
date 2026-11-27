using System.ComponentModel.DataAnnotations;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;

namespace FlashMemo.ViewModel.Bases;

public enum FileFormat { txt, csv, flamePkg }
public enum NoteSeparator { Tab, Enter, Space, SemiColon }

public abstract partial class ImportExportVMBase(Action cancel, string path = "") : PopupVMBase(cancel)
{
    #region properties
    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidateFormatProperty))]
    public partial FileFormat Format { get; set; } = FileFormat.txt;

    [ObservableProperty] [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Choose a file.")]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidatePathProperty))]
    public partial string MyPath { get; set; } = path;

    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidateFieldSeparatorProperty))]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidateSeparatorsProperty))]
    public partial string FieldSeparator { get; set; } = " - ";

    [ObservableProperty] [NotifyDataErrorInfo]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidateNoteSeparatorProperty))]
    [CustomValidation(typeof(ImportExportVMBase), nameof(ValidateSeparatorsProperty))]
    public partial NoteSeparator NoteSep { get; set; } = NoteSeparator.Enter;
    #endregion

    #region validation
    internal bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    partial void OnFormatChanged(FileFormat value) => HandleFormatChanged(value);

    protected virtual void HandleFormatChanged(FileFormat value)
    {
        ValidateProperty(MyPath, nameof(MyPath));
        ValidateProperty(FieldSeparator, nameof(FieldSeparator));
        ValidateProperty(NoteSep, nameof(NoteSep));
    }

    partial void OnFieldSeparatorChanged(string value)
        => ValidateProperty(NoteSep, nameof(NoteSep));

    partial void OnNoteSepChanged(NoteSeparator value)
        => ValidateProperty(FieldSeparator, nameof(FieldSeparator));

    // CustomValidation attributes require public static methods; delegate to overridable instance methods.
    public static ValidationResult? ValidateFormatProperty(FileFormat value, ValidationContext context)
        => ((ImportExportVMBase)context.ObjectInstance).ValidateFormat(value);

    public static ValidationResult? ValidatePathProperty(string? value, ValidationContext context)
        => ((ImportExportVMBase)context.ObjectInstance).ValidatePath(value);

    public static ValidationResult? ValidateFieldSeparatorProperty(string? value, ValidationContext context)
        => ((ImportExportVMBase)context.ObjectInstance).ValidateFieldSeparator(value);

    public static ValidationResult? ValidateNoteSeparatorProperty(NoteSeparator value, ValidationContext context)
        => ((ImportExportVMBase)context.ObjectInstance).ValidateNoteSeparator(value);

    public static ValidationResult? ValidateSeparatorsProperty(object? value, ValidationContext context)
        => ((ImportExportVMBase)context.ObjectInstance).ValidateSeparators();

    protected virtual ValidationResult? ValidateFormat(FileFormat value)
        => Enum.IsDefined(value)
            ? ValidationResult.Success
            : new ValidationResult("Choose a supported file format.");

    protected virtual ValidationResult? ValidateNoteSeparator(NoteSeparator value)
    {
        if (Format == FileFormat.txt && !Enum.IsDefined(value))
            return new ValidationResult("Choose a note separator.");

        return ValidationResult.Success;
    }

    protected virtual ValidationResult? ValidateFieldSeparator(string? value)
    {
        if (Format == FileFormat.txt && string.IsNullOrEmpty(value))
            return new ValidationResult("Enter a field separator.");

        return ValidationResult.Success;
    }

    protected virtual ValidationResult? ValidateSeparators()
        => Format == FileFormat.txt && Enum.IsDefined(NoteSep)
        && FieldSeparator == NoteSep.ToChar().ToString()
            ? new ValidationResult("Field and note separators must be different.")
            : ValidationResult.Success;

    protected virtual ValidationResult? ValidatePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success; // Empty paths are handled by [Required].

        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return new ValidationResult("Enter a valid file path.");

        string fileName = Path.GetFileName(value);
        if (string.IsNullOrWhiteSpace(fileName) || Directory.Exists(value)
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return new ValidationResult("Choose a filename, not a folder, with valid characters.");

        if (Format == FileFormat.txt
            && !string.Equals(Path.GetExtension(value), ".txt", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult("The text filename must end in .txt.");

        return ValidationResult.Success;
    }
    #endregion
}