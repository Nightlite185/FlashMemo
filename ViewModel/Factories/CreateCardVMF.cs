using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardRepo cr, DeckSelectVMF dsVMF, ILastSessionService lss, 
                            IVMEventBus bus, IDeckRepo repo, CardTagsVMF cardTagsVMF,
                            ILogger<CreateCardVM> logger, IDialogService dialogs)
{
    public async Task<CreateCardVM> CreateAsync(IDeckMeta targetDeck)
    {
        CreateCardVM vm = new(
            cr, targetDeck, dsVMF,
            lss, bus, repo,
            logger, dialogs);

        var ctVM = await cardTagsVMF.CreateAsync(
            userId: targetDeck.UserId,
            host: vm);

        vm.Initialize(ctVM);
        return vm;
    }
}