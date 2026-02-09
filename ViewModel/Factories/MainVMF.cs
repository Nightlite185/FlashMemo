using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(IDisplayControl ns, IDeckRepo dr, ILastSessionService lss)
{
    private readonly IDisplayControl displayCtrl = ns;
    private readonly ILastSessionService lastSession = lss;
    private readonly IDeckRepo deckRepo = dr;
    public MainVM Create(long userId)
    {
        // any logic that runs to initialize mainVM goes here.
        return new(displayCtrl, lastSession, deckRepo, userId);
    }
}