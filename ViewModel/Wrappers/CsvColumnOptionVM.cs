using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CsvColumnOptionVM(
    string name, 
    Expression<Func<CsvExportDTO, string?>> propSelector, 
    bool isDefault = false) 
    : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isDefault;
    public bool IsDefault { get; } = isDefault;
    public string Name { get; } = name;
    public Expression<Func<CsvExportDTO, string?>> PropSelector { get; } = propSelector;
}
