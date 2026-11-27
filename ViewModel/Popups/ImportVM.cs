using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class ImportVM(Action cancel, SafetyNet net, IImportService impService): PopupVMBase(cancel)
{
    #region properties
    [ObservableProperty]
    public partial string ImportPath { get; set; }

    [ObservableProperty]
    public partial FileFormat Format { get; set; }
    #endregion

    #region methods
    protected override async Task Confirm()
    {
        bool success = Format switch
        {
            // FileFormat.txt => impService.ImportTxtAsync(),
            // FileFormat.csv => impService.ImportCsvAsync(),
            // FileFormat.flamePkg => impService.ImportFlameAsync(),

            _ => throw new InvalidEnumArgumentException()
        };

        if (success) Close();
    }
    #endregion
}