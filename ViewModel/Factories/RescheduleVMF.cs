using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class RescheduleVMF(ILogger<RescheduleVM> logger, IDialogService dialogs, SafetyNet net)
{
    public RescheduleVM Create(Func<IRescheduleData, Task> confirm, Action cancel)
    {
        return new(confirm, cancel, 
            logger, dialogs, net);
    }
}