using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
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
        var vm = await decksVMF
            .CreateAsync(userId);
        
        vm.OnReviewNavRequest += deck => SwitchToReview(userId, deck);
        
        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public async Task SwitchToReview(long userId, IDeckMeta deck)
    {
        var vm = await reviewVMF
            .CreateAsync(userId, deck);

        vm.OnDecksNavRequest += () => SwitchToDecks(userId);

        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public Task SwitchToStats(long userId)
    {
        throw new NotImplementedException();
    }

    private void WireEvents(object vm)
    {
        if (vm is INavRequestSender navVM && host is INavRequestSender parent)
            parent.RegisterNavBubbling(navVM);
    }
}