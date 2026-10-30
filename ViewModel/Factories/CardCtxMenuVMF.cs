using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Popups;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, IVMEventBus bus,
                            DeckSelectVMF dsVMF, IUserOptionsService userOptionsService, ILogger<CardCtxMenuVM> logger,
                            IDialogService dialogs, ILogger<RescheduleVM> rescheduleLogger, SafetyNet net)
{
    public CardCtxMenuVM Create(IPopupHost pph, ICtxMenuHost host, long userId)
    {
        return new(
            cs, cr, pph, dsVMF,
            bus, userOptionsService,
            userId, host,
            logger, dialogs, rescheduleLogger, net);
    }
}
