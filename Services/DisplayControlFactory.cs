using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class DisplayControlFactory(DecksVMF decksVMF, ReviewVMF reviewVMF, StatsVMF statsVMF)
{
    public IDisplayControl Create(IDisplayHost host)
    {
        return new DisplayControl(
            host, decksVMF, 
            reviewVMF, statsVMF);
    }
}