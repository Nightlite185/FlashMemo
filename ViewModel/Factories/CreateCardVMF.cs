using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardRepo cr, DeckSelectVMF dsVMF, ILastSessionService lss, 
                            IVMEventBus bus, IDeckRepo repo, CardTagsVMF cardTagsVMF,
                            ILogger<CreateCardVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<CreateCardVM?> TryCreateAsync(IDeckMeta targetDeck)
    {
        var res = await net.TryUnsafe(
            action: async () =>
            {
                CreateCardVM vm = new(
                    cr, targetDeck, dsVMF,
                    lss, bus, repo,
                    logger, dialogs, net);

                var ctVM = await cardTagsVMF.CreateAsync(
                    userId: targetDeck.UserId,
                    host: vm);

                vm.Initialize(ctVM);
                return vm;
            },
            actionTried: "open the card creation window",
            retry: true);

        return res.Success ? res.Value : null;
    }
}