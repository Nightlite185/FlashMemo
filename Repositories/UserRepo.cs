using System.Collections.ObjectModel;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.WrapperVMs;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class UserRepo(IDbContextFactory<AppDbContext> dbFactory): DbDependentClass(dbFactory), IUserRepo
{
    public async Task<ICollection<UserEntity>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<UserEntity> GetByIdAsync(long userId)
    {
        throw new NotImplementedException();
    }

    public Task Remove(long userId)
    {
        throw new NotImplementedException();
    }

    public Task SaveEdited(UserEntity edited)
    {
        throw new NotImplementedException();
    }
}