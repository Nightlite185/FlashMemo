using FlashMemo.Model.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.Fakes.Model;

public class FakeDbFactory: IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var con = new SqliteConnection(
            "Data Source=:memory:");
        
        con.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(con).Options;
        
        return new AppDbContext(opt);
    }
}