using AutoMapper;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class UserOptionsMenuVMF(IUserOptionsService userOptRepo, IMapper mapper)
{
    public async Task<UserOptionsMenuVM> CreateAsync(long userId)
    {
        var vm = new UserOptionsMenuVM(
            userId, userOptRepo, mapper);

        await vm.InitAsync();
        return vm;
    }
}