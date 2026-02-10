using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(DisplayControlFactory dcFactory, IDeckRepo dr, ILastSessionService lss)
{
    private readonly DisplayControlFactory displayControlFactory = dcFactory;
    private readonly ILastSessionService lastSession = lss;
    private readonly IDeckRepo deckRepo = dr;
    public MainVM Create(long userId)
    {
        MainVM vm = new(
            lastSession, deckRepo, userId);

        var dc = displayControlFactory.Create(vm);

        vm.Initialize(dc);
        return vm;
    }
}