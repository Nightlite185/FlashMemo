using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class MainVMF(DisplayControlFactory dcFactory, IDeckRepo dr, ILastSessionService lss, 
                    IVMEventBus bus, ILogger<MainVM> logger, IDialogService dialogs, SafetyNet net)
{
    private readonly DisplayControlFactory displayControlFactory = dcFactory;
    private readonly IVMEventBus eventBus = bus;
    private readonly ILastSessionService lastSession = lss;
    private readonly IDeckRepo deckRepo = dr;
    public async Task<MainVM> CreateAsync(long userId)
    {
        MainVM vm = new(
            lastSession, deckRepo,
            eventBus, userId,
            logger, dialogs, net);

        var dc = displayControlFactory.Create(vm);

        await vm.InitAsync(dc);
        return vm;
    }
}
