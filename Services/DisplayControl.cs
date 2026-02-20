using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.Services;
public class DisplayControl(IDisplayHost hostVM, DecksVMF dVMF, ReviewVMF rVMF): IDisplayControl
{
    private readonly ReviewVMF reviewVMF = rVMF;
    private readonly DecksVMF decksVMF = dVMF;
    private readonly IDisplayHost host = hostVM;

    public async Task SwitchToDecks(long userId)
    {
        UnsubEvents();
        
        var vm = await decksVMF
            .CreateAsync(userId);
        
        vm.OnReviewNavRequest += deck => SwitchToReview(userId, deck);
        host.NotifyRefresh += vm.SyncDeckTree;
        
        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public async Task SwitchToReview(long userId, IDeckMeta deck)
    {
        UnsubEvents();

        var vm = await reviewVMF
            .CreateAsync(userId, deck);

        vm.OnDecksNavRequest += () => SwitchToDecks(userId);

        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public Task SwitchToStats(long userId)
    {
        UnsubEvents();

        throw new NotImplementedException();
    }

    private void WireEvents(object vm)
    {
        if (vm is NavBaseVM navVM && host is NavBaseVM parent)
            parent.RegisterNavBubbling(navVM);
    }

    private void UnsubEvents()
    {
        if (host.CurrentDisplay is DecksVM vm)
            host.NotifyRefresh -= vm.SyncDeckTree;
    }
}