using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.ViewModel.Factories;

public class ExportVMF(ICardQueryService cardQService, INoteSerializer ser)
{
    public ExportVM Create(long userId, IEnumerable<long> deckIds, Action cancel)
    {
        var vm = new ExportVM(
            userId, cardQService,
            ser, cancel, deckIds);

        vm.ErrorsChanged += (_, _) => vm.ConfirmCommand.NotifyCanExecuteChanged();
        vm.Validate();

        return vm;
    }
}
