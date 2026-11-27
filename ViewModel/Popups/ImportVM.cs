using System.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class ImportVM(Action cancel, SafetyNet net, IImportService impService): ImportExportVMBase(cancel)
{
    #region methods
    protected override async Task Confirm()
    {
        if (!Validate())
            return;

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