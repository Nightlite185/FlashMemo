using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;
public class DisplayControl(IDisplayHost host, DecksVMF decksVMF, ReviewVMF reviewVMF, StatsVMF statsVMF): IDisplayControl
{
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

    public async Task SwitchToStats(long userId)
    {
        var vm = await statsVMF
            .CreateAsync(userId);

        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    private void WireEvents(object vm)
    {
        if (vm is INavRequestSender navVM && host is INavRequestSender parent)
            parent.RegisterNavBubbling(navVM);
    }
}