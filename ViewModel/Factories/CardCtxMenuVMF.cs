using AutoMapper;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, IVMEventBus bus, 
                            DeckSelectVMF dsVMF, IUserOptionsService userOptionsService, 
                            ILogger<CardCtxMenuVM> logger, IDialogService dialogs, 
                            SafetyNet net, IMapper mapper, RescheduleVMF rescheduleVMF)
{
    public CardCtxMenuVM Create(IPopupHost pph, ICtxMenuHost host, long userId)
    {
        return new(
            cs, cr, pph, dsVMF,
            bus, userOptionsService,
            userId, host, logger, 
            dialogs, net, mapper, 
            rescheduleVMF);
    }
}