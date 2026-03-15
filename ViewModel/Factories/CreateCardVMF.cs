using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardRepo cr, DeckSelectVMF dsVMF, ILastSessionService lss, 
                            IVMEventBus bus, IDeckRepo repo, CardTagsVMF cardTagsVMF)
{
    public async Task<CreateCardVM> CreateAsync(IDeckMeta targetDeck)
    {
        CreateCardVM vm = new(
            cr, targetDeck, dsVMF,
            lss, bus, repo);

        var ctVM = await cardTagsVMF.CreateAsync(
            userId: targetDeck.UserId,
            host: vm);

        vm.Initialize(ctVM);
        return vm;
    }
}