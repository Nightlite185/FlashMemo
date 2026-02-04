using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class UserRepo(IDbContextFactory<AppDbContext> dbFactory): DbDependentClass(dbFactory), IUserRepo
{
    public async Task<ICollection<UserEntity>> GetAllAsync()
    {
        var db = GetDb;

        return await db.Users
            .AsNoTracking()
            .ToArrayAsync();
    }
    public async Task<UserEntity> GetByIdAsync(long userId)
    {
        var db = GetDb;

        return await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == userId);
    }
    public async Task Remove(long userId)
    {
        var db = GetDb;

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync();
    }
    public async Task Rename(long userId, string newName)
    {
        var db = GetDb;

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(
                u => u.Name, newName
            ));
    }
    public async Task CreateNew(UserEntity toAdd)
    {
        var db = GetDb;

        await db.Users.AddAsync(toAdd);
        await db.SaveChangesAsync();
    }
}