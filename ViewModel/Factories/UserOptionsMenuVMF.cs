using AutoMapper;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class UserOptionsMenuVMF(IUserOptionsService userOptRepo, IMapper mapper, IVMEventBus bus)
{
    public async Task<UserOptionsMenuVM> CreateAsync(long userId)
    {
        var vm = new UserOptionsMenuVM(
            userId, userOptRepo, 
            mapper, bus);

        await vm.InitAsync();
        return vm;
    }
}