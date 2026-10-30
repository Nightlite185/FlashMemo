using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CreateDeckVMF(ILogger<CreateDeckVM> logger, IDialogService dialogs, SafetyNet net)
{
    public CreateDeckVM Create(Func<string, DeckNode?, Task> confirm, 
                                Action cancel, DeckNode? parent = null)
        => new(confirm, cancel, logger, dialogs, net, parent);
}
