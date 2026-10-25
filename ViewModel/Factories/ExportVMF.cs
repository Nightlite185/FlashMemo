using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Factories;

public class ExportVMF(ICardQueryService cardQService, IExportService exporter,
    ICountingService counter, IDialogService dialogs)
{
    public async Task<ExportVM?> CreateAsync(long userId, IEnumerable<DeckNode> deckTree, Action cancel)
    {
        var selectedRoots = deckTree.GetSelectedRoots();
        var flatIds = selectedRoots
            .Flatten()
            .Select(d => d.Id)
            .Distinct()
            .ToArray();

        if (await counter.AllCardsInDecks(flatIds) == 0)
        {
            dialogs.ShowNoCardsFoundToExport();
            return null;
        }

        var vm = new ExportVM(
            userId, cardQService, 
            exporter, dialogs, cancel, 
            flatIds, selectedRoots);

        vm.ErrorsChanged += (_, _) => vm.ConfirmCommand.NotifyCanExecuteChanged();
        vm.Validate();

        return vm;
    }
}
