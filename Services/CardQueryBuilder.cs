using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;

public class CardQueryBuilder(IDbContextFactory<AppDbContext> factory): DbDependentClass(factory)
{
    
}