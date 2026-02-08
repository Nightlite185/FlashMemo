using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories;

public class UserOptionsRepo(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory), IUserOptionsRepo
{
    
}