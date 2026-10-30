using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IDeckRepo deckRepo, IDeckTreeBuilder deckBuilder, 
                    IVMEventBus bus, HeatmapVMF heatVMF, ILastSessionService lastSession,
                    IUserOptionsService userOptionsService, ExportVMF exportVMF,
                    ILogger<DecksVM> logger, IDialogService dialogs, CreateDeckVMF createDeckVMF, SafetyNet net)
{
    public async Task<DecksVM> CreateAsync(long userId)
    {
        var heatVM = await heatVMF
            .CreateAsync(userId);

        var vm = new DecksVM(
            deckRepo, deckBuilder,
            heatVM, lastSession,
            userOptionsService, userId,
            bus, exportVMF,
            logger, dialogs, createDeckVMF, net);

        await vm.InitAsync();
        return vm;
    }
}
