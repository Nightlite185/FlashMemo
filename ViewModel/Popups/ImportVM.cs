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
        if (!await net.TryUnsafe(
            action: () => impService.ImportAsync(ImportPath, Format),
            actionTried: "import content from given path in chosen format.",
            retry: false))
            return;

        Close();
    }
    #endregion
}
