using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.Services;

public class UserVMBuilder: IUserVMBuilder
{
    public async Task<IEnumerable<UserVM>> BuildAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<UserVM> BuildByIdAsync(long userId)
    {
        throw new NotImplementedException();
    }
}