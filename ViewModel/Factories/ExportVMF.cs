using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class ExportVMF(ICardQueryService cardQService, IExportService exporter,
                        ICountingService counter, IDialogService dialogs,
                        ILogger<ExportVM> logger, SafetyNet net)
{
    public async Task<ExportVM?> TryCreateAsync(long userId, IEnumerable<DeckNode> deckTree, Action cancel)
    {
        var res = await net.TryUnsafe(
            action: async () =>
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
                    flatIds, selectedRoots,
                    logger, net);

                vm.ErrorsChanged += (_, _) => vm.ConfirmCommand.NotifyCanExecuteChanged();
                
                foreach (var column in vm.CsvColumns)
                {
                    column.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(CsvColumnOptionVM.IsSelected))
                            vm.ValidateCsvColumns();
                    };
                }
                vm.Validate();

                return vm;
            },
            actionTried: "open the export menu",
            retry: true);

        return res.Success ? res.Value : null;
    }
}