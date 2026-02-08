using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;
public class DisplayControl(IDisplayHost hostVM, DecksVMF dVMF, ReviewVMF rVMF): IDisplayControl
{
    // private readonly IServiceProvider sp = isp;
    private readonly ReviewVMF reviewVMF = rVMF;
    private readonly DecksVMF decksVMF = dVMF;
    private readonly IDisplayHost host = hostVM;

    public async Task SwitchToDecks(long userId)
    {
        host.CurrentDisplay = await 
            decksVMF.CreateAsync(userId);
    }

    public async Task SwitchToReview(long userId, long deckId)
    {
        host.CurrentDisplay = await 
            reviewVMF.CreateAsync(userId, deckId);
    }
}
