using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class DisplayControlFactory(DecksVMF dVMF, ReviewVMF rVMF)
{
    private readonly ReviewVMF reviewVMF = rVMF;
    private readonly DecksVMF decksVMF = dVMF;
    
    public IDisplayControl Create(IDisplayHost host)
    {
        return new DisplayControl(
            host, decksVMF, reviewVMF);
    }
}