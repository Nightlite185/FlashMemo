using FlashMemo.Repositories;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class UserOptionsVMF(IUserOptionsRepo uor)
{
    private readonly IUserOptionsRepo userOptRepo = uor;

    public async Task<UserOptionsVM> CreateAsync(long userId)
    {
        var vm = new UserOptionsVM(userId, userOptRepo);

        await vm.InitAsync();
        return vm;
    }
}