using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;

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

        vm.OnReviewShowRequest += deck 
            => SwitchToReview(host.UserId, deck);
        
        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public async Task SwitchToReview(long userId, IDeckMeta deck)
    {
        var vm = await reviewVMF
            .CreateAsync(userId, deck);

        WireEvents(vm);
        host.CurrentDisplay = vm;
    }

    public Task SwitchToStats(long userId)
    {
        throw new NotImplementedException();
    }

    private void WireEvents(object vm)
    {
        if (vm is NavBaseVM navVM && host is NavBaseVM parent)
            parent.RegisterBubbling(navVM);
    }
}