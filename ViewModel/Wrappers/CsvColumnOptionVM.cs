using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CsvColumnOptionVM(
    string name, 
    Expression<Func<CsvExportDTO, string?>> propSelector, 
    bool isDefault = false,
    bool isRequired = false)
    : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isDefault || isRequired;
    public bool IsDefault { get; } = isDefault;
    public bool IsRequired { get; } = isRequired;
    public string Name { get; } = name;
    public Expression<Func<CsvExportDTO, string?>> PropSelector { get; } = propSelector;
}