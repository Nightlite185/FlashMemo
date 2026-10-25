using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Factories;

public class ExportVMF(ICardQueryService cardQService, INoteSerializer ser, ICountingService counter)
{
    public async Task<ExportVM?> CreateAsync(long userId, IEnumerable<DeckNode> deckTree, Action cancel)
    {
        var flatIds = deckTree
            .Flatten()
            .Select(d => d.Id)
            .Distinct()
            .ToArray();

        if (await counter.AllCardsInDecks(flatIds) == 0)
        {
            DialogService.ShowNoCardsFoundToExport();
            return null;
        }

        var vm = new ExportVM(
            userId, cardQService, ser,
            cancel, flatIds, deckTree.ToArray());

        vm.ErrorsChanged += (_, _) => vm.ConfirmCommand.NotifyCanExecuteChanged();
        vm.Validate();

        return vm;
    }
}
