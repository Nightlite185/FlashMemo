using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(IDisplayControl ns)
{
    private readonly IDisplayControl displayCtrl = ns;
    public MainVM Create(long userId)
    {
        // any logic that runs to initialize mainVM goes here.
        return new(displayCtrl, userId);
    }
}