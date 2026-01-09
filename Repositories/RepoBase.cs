using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Repositories
{
    public abstract class DbDependentClass(IDbContextFactory<AppDbContext> dbFactory)
    {
        protected readonly IDbContextFactory<AppDbContext> dbFactory = dbFactory;
        protected AppDbContext GetDb => dbFactory.CreateDbContext();
        protected static string IdNotFoundMsg(string NotFound) => $"{NotFound} with this id was not found in database.";
    }
}