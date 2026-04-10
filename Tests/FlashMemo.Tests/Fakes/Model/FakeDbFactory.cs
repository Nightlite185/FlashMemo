using FlashMemo.Model.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.Fakes.Model;

public class FakeDbFactory: IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection con;
    public FakeDbFactory()
    {
        con = new("Data Source=:memory:");
        con.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }
    public AppDbContext CreateDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(con).Options;
        
        return new AppDbContext(opt);
    }

    public void Dispose()
    {
        con.Dispose();
    }
}