using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.ViewModel.Factories;

public class ExportVMF(ICardQueryService cardQService, INoteSerializer ser, ICountingService counter)
{
    public async Task<ExportVM?> CreateAsync(long userId, IEnumerable<long> deckIds, Action cancel)
    {
        if (await counter.AllCardsInDecks(deckIds) == 0)
        {
            DialogService.ShowNoCardsFoundToExport();
            return null;
        }

        var vm = new ExportVM(
            userId, cardQService,
            ser, cancel, deckIds);

        vm.ErrorsChanged += (_, _) => vm.ConfirmCommand.NotifyCanExecuteChanged();
        vm.Validate();

        return vm;
    }
}
