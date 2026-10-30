using AutoMapper;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class UserOptionsMenuVMF(IUserOptionsService userOptRepo, IMapper mapper, IVMEventBus bus,
                                ILogger<UserOptionsMenuVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<UserOptionsMenuVM> CreateAsync(long userId)
    {
        var vm = new UserOptionsMenuVM(
            userId, userOptRepo, 
            mapper, bus,
            logger, dialogs, net);

        await vm.InitAsync();
        return vm;
    }
}
